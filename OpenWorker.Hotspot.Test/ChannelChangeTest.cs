using System.Buffers.Binary;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Channel;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Hotspot.Modules.Channels.Enums;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ChannelChangeTest
{
    private const short Location = 10003;

    private const short Gate = 7;

    /// <summary>
    /// The client continues a change of channel only from its answer: on success it takes the map of the answer (with the new
    /// channel) and asks what is around it, which then holds the people of the new channel; the people it leaves are gone
    /// for it and it for them. The same channel, a channel the server does not run and a full one are refused with the
    /// client's lines, and the player stays where it was.
    /// </summary>
    [TestMethod]
    public async Task AChangeOfChannelIsAnsweredAndTheBadOnesAreRefused()
    {
        using var world = World.Create();
        var provider = ChannelMembers.Provider();
        var channels = new ServiceChannels(Options.Create(new ChannelOptions { From = 1, To = 3 }), world);
        var server = new ServerChannels(
            Options.Create(new InstanceOptions { Location = Location, PublicHost = "127.0.0.1", PublicPort = 10011 }),
            Options.Create(new GateOptions { Id = Gate }),
            null!,
            channels,
            world);

        var next = 0;

        Player Join(short index)
        {
            var number = next++;
            var connection = new RecordingConnection();
            var entity = ChannelMembers.Create(world, provider, connection, number);
            var actor = 1000 + number;

            world.Set(entity, new ActorComponent(actor, ActorType.User));
            world.Set(entity, new ClaimsComponent(account: actor, salt: 1));
            world.Set(entity, new WorldComponent { Location = Location, Position = new Vector3(100f, 100f, 0f), Rotation = 0f, Jump = 0 });

            channels.Join(index, entity);

            return new Player(entity, actor, connection);
        }

        var mover = Join(0);
        var left = Join(0);
        var waiting = Join(1);

        // The third channel is full.
        for (var i = 0; i <= ChannelModuleDefinitions.Workload.Full; i++)
        {
            Join(2);
        }

        Assert.AreEqual(ChannelWorkload.Full, channels[2].Workload);

        await MetricAssertions.UntilAsync(() => mover.Received().Any(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.InInfoPc)), "The mover is told about the one beside it.");
        DistrictHarness.Forget(mover, left, waiting);

        Assert.AreEqual(ChannelSwitchResult.Same, server.Switch(mover.Entity, 1));
        Assert.AreEqual(ChannelSwitchResult.NotFound, server.Switch(mover.Entity, 9));
        Assert.AreEqual(ChannelSwitchResult.Full, server.Switch(mover.Entity, 3));

        await MetricAssertions.UntilAsync(() => Refusals(mover).Length == 3, "Every refusal is answered.");
        CollectionAssert.AreEqual(new ushort[] { ServerChannels.CannotEnterCode, ServerChannels.NotFoundCode, ServerChannels.CannotEnterCode }, Refusals(mover));
        Assert.IsTrue(channels[0].Contains(mover.Entity), "A refused player stays in its channel.");

        DistrictHarness.Forget(mover);

        Assert.AreEqual(ChannelSwitchResult.Ok, server.Switch(mover.Entity, 2));

        await MetricAssertions.UntilAsync(() => Answers(mover).Length == 1, "The change is answered.");

        var answer = Answers(mover)[0].Body;
        var map = BinaryPrimitives.ReadUInt64LittleEndian(answer.AsSpan(16));

        Assert.AreEqual(0, BinaryPrimitives.ReadInt32LittleEndian(answer.AsSpan(answer.Length - 4)), "The result is a success.");
        Assert.AreEqual(0, answer[^6], "The player stays on this server.");
        Assert.AreEqual(2UL, (map >> 24) & 0xFF, "The map of the answer names the new channel.");
        Assert.AreEqual((ulong)Location, (map >> 32) & 0xFFFF);
        Assert.AreEqual((ulong)Gate, (map >> 48) & 0xFFFF);

        await MetricAssertions.UntilAsync(() => waiting.Arrivals().Length == 1 && left.Departures().Contains(mover.Actor), "The two channels are told.");
        await MetricAssertions.UntilAsync(() => mover.Departures().Contains(left.Actor), "The people of the old channel are gone for the mover.");

        // What the client asks next, the people around it, are those of the new channel.
        DistrictHarness.Forget(mover);
        channels[1].SendOthers(mover.Entity, world);

        await MetricAssertions.UntilAsync(
            () => mover.Received().Any(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.OtherInfosPc)),
            "The mover gets the people around it.");
        Assert.IsTrue(channels[1].Sees(mover.Entity, waiting.Entity));
        Assert.IsFalse(channels[0].Contains(mover.Entity));
    }

    private static PlayerFrame[] Answers(Player player) =>
        player.Received().Where(e => e.Is(GroupOpcode.Channel, (byte)ChannelOpcode.Change)).ToArray();

    private static ushort[] Refusals(Player player) => player.Received()
        .Where(e => e.Is(GroupOpcode.Channel, (byte)ChannelOpcode.Change | 0x80))
        .Select(e => BinaryPrimitives.ReadUInt16LittleEndian(e.Body))
        .ToArray();
}
