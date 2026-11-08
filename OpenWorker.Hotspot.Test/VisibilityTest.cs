using System.Buffers.Binary;
using System.Numerics;
using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.DistrictServer.Services;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A district with one channel and its movement handler, whose players stand on connections that record what they get.
/// </summary>
internal sealed class DistrictHarness : IDisposable
{
    private readonly ArchComponentProvider _provider = ChannelMembers.Provider();

    private int _next;

    public DistrictHarness()
    {
        Channels = ChannelMembers.Channels(Ecs);
        Movement = new MovementService(Ecs, Channels);
    }

    public World Ecs { get; } = World.Create();

    public ServiceChannels Channels { get; }

    public ServiceChannel Channel => Channels[0];

    public MovementService Movement { get; set; }

    /// <summary>
    /// A player that has entered the game at the place and joined the channel.
    /// </summary>
    public Player Join(float x, float y)
    {
        var index = _next++;
        var connection = new RecordingConnection();
        var entity = ChannelMembers.Create(Ecs, _provider, connection, index);
        var actor = 1000 + index;

        Ecs.Set(entity, new ActorComponent(actor, ActorType.User));
        Ecs.Set(entity, new ClaimsComponent(account: actor, salt: 1));
        Ecs.Set(entity, new WorldComponent { Location = 10003, Position = new Vector3(x, y, 0f), Rotation = 0f, Jump = 0 });

        Channels.Join(0, entity);

        return new Player(entity, actor, connection);
    }

    /// <summary>
    /// The player says it runs to the place.
    /// </summary>
    public ValueTask MoveAsync(Player player, float x, float y)
    {
        return Movement.OnHandleAsync(new ServiceHandleContext(player.Entity, CancellationToken.None), Request(player.Actor, x, y));
    }

    /// <summary>
    /// Sends a message to the whole channel and waits until every player has it: whatever was queued before it has
    /// arrived too, and what has not arrived by then was never sent.
    /// </summary>
    public async Task SettleAsync(params Player[] players)
    {
        Channel.Broadcast(new Marker());

        await MetricAssertions.UntilAsync(
            () => players.All(e => e.Received().Any(f => (short)f.Opcode == Marker.Code)),
            "Every player gets the marker.");
    }

    /// <summary>
    /// <see cref="SettleAsync"/>, then forgets what the players got: what joining told them, for example.
    /// </summary>
    public async Task ResetAsync(params Player[] players)
    {
        await SettleAsync(players);

        foreach (var player in players)
        {
            player.Forget();
        }
    }

    public static MovementMoveRequest Request(int actor, float x, float y)
    {
        Span<byte> bytes = stackalloc byte[4 + 8 + (6 * 4) + 1 + 4 + 4 + 1];

        var writer = new PacketWriter(bytes);

        writer.WriteInt32(actor);
        writer.WriteInt64(0x0001_2713_0000_0000);
        writer.WriteSingle(x);
        writer.WriteSingle(y);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteSingle(x);
        writer.WriteSingle(y);
        writer.WriteByte(1);
        writer.WriteSingle(0f);
        writer.WriteSingle(560f);
        writer.WriteByte(0);

        var reader = new PacketReader(bytes);

        return MovementMoveRequest.Read(ref reader);
    }

    public void Dispose()
    {
        Ecs.Dispose();
    }

    /// <summary>
    /// A message on an opcode no real message uses, with an empty body.
    /// </summary>
    private sealed class Marker : IResponseHotspotMessage
    {
        public static readonly MessageOpcode Code = new(GroupOpcode.System, (SystemOpcode)0x7B);

        public MessageOpcode Opcode => Code;

        public void Write(BinaryWriter writer)
        {
        }
    }
}

/// <param name="Opcode">What the frame is.</param>
/// <param name="Body">The decrypted body after the opcode.</param>
internal readonly record struct PlayerFrame(MessageOpcode Opcode, byte[] Body)
{
    public static PlayerFrame Open(byte[] frame)
    {
        var header = new MessageHeader(frame);
        var body = frame.AsSpan(MessageHeader.HeaderSize, header.ContentLength).ToArray();

        MessageEncryption.Exchange(body.AsSpan(), header.Version);

        return new PlayerFrame(new MessageOpcode(body[0], body[1]), body[2..]);
    }

    public bool Is(GroupOpcode group, byte command) => Opcode.Group == (byte)group && Opcode.Command == command;

    /// <summary>
    /// The actor a message about one actor starts with.
    /// </summary>
    public int Actor => new ActorComponent(BinaryPrimitives.ReadInt32LittleEndian(Body)).Identifier;

    /// <summary>
    /// The actors of a list with a one-byte count, as <c>WorldOutInfoPc</c> sends them.
    /// </summary>
    public int[] Actors
    {
        get
        {
            var body = Body;

            return Enumerable.Range(0, body[0])
                .Select(e => new ActorComponent(BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(1 + (4 * e)))).Identifier)
                .ToArray();
        }
    }
}

internal sealed class Player(Entity entity, int actor, RecordingConnection connection)
{
    private int _seen;

    public Entity Entity { get; } = entity;

    public int Actor { get; } = actor;

    /// <summary>
    /// What arrived since the last <see cref="Forget"/>.
    /// </summary>
    public PlayerFrame[] Received() => connection.Frames.Skip(_seen).Select(PlayerFrame.Open).ToArray();

    public void Forget() => _seen = connection.Frames.Length;

    public PlayerFrame[] Moves() => Received().Where(e => e.Is(GroupOpcode.Move, (byte)MoveOpcode.MoveBt)).ToArray();

    public PlayerFrame[] Arrivals() => Received().Where(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.InInfoPc)).ToArray();

    public int[] Departures() => Received()
        .Where(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.OutInfoPc))
        .SelectMany(e => e.Actors)
        .ToArray();
}

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class VisibilityTest
{
    private const float Cell = OpenWorker.Extensions.WorldOptions.DefaultVisibilityRadius;

    /// <summary>
    /// The criterion of phase 3: a move goes to the mover and the players who see it, not to the whole channel, and the
    /// list of the players around that the client asks for once it has loaded the map holds only those it sees.
    /// </summary>
    [TestMethod]
    public async Task AMoveIsHeardOnlyByThePlayersWhoSeeTheMover()
    {
        using var district = new DistrictHarness();

        var mover = district.Join(100f, 100f);
        var near = district.Join(Cell * 1.5f, 100f);
        var far = district.Join(Cell * 4f, 100f);

        await district.ResetAsync(mover, near, far);

        Assert.IsTrue(district.Channel.Sees(near.Entity, mover.Entity));
        Assert.IsFalse(district.Channel.Sees(far.Entity, mover.Entity));

        await district.MoveAsync(mover, 500f, 100f);
        await WaitForMovesAsync(mover, near);

        Assert.AreEqual(mover.Actor, near.Moves().Single().Actor, "The player in the next cell hears the move.");
        Assert.AreEqual(mover.Actor, mover.Moves().Single().Actor, "The mover hears its own move.");

        await district.SettleAsync(far);

        Assert.IsEmpty(far.Moves(), "A player three cells away does not hear the move.");

        mover.Forget();
        district.Channel.SendOthers(mover.Entity, district.Ecs);

        var list = await FirstAsync(mover, GroupOpcode.World, (byte)WorldOpcode.OtherInfosPc);

        Assert.AreEqual(1, BinaryPrimitives.ReadInt16LittleEndian(list.Body), "The list of the players around holds the one in sight.");
    }

    /// <summary>
    /// Crossing the edge of a cell: the player who comes into sight and the mover are told about each other, the one who
    /// goes out of sight and the mover are told that the other is gone, and the player who stays in sight hears nothing new.
    /// </summary>
    [TestMethod]
    public async Task CrossingTheEdgeOfACellTellsWhoComesIntoSightAndWhoGoesOut()
    {
        using var district = new DistrictHarness();

        var mover = district.Join(100f, 100f);
        var behind = district.Join(200f, 100f);
        var ahead = district.Join((Cell * 2f) + 100f, 100f);

        await district.ResetAsync(mover, behind, ahead);

        // Into the next cell: the player two cells ahead comes into sight, the one behind is still in sight.
        await district.MoveAsync(mover, Cell + 100f, 100f);
        await WaitForMovesAsync(mover, behind, ahead);

        Assert.HasCount(1, ahead.Arrivals(), "The player ahead is told about the mover.");
        Assert.HasCount(1, mover.Arrivals(), "The mover is told about the player ahead.");
        Assert.IsEmpty(behind.Arrivals(), "The player behind saw the mover already.");
        Assert.IsEmpty(behind.Departures());

        foreach (var player in new[] { mover, behind, ahead })
        {
            player.Forget();
        }

        // Two cells away from the start: the player behind goes out of sight.
        await district.MoveAsync(mover, (Cell * 2f) + 100f, 100f);
        await WaitForMovesAsync(mover, ahead);
        await district.SettleAsync(behind);

        CollectionAssert.AreEqual(new[] { mover.Actor }, behind.Departures(), "The player behind is told that the mover is gone.");
        CollectionAssert.AreEqual(new[] { behind.Actor }, mover.Departures(), "The mover is told that the player behind is gone.");
        Assert.IsEmpty(behind.Moves(), "The player out of sight does not hear the move that took the mover away.");
        Assert.IsEmpty(ahead.Arrivals(), "The player ahead saw the mover already.");
    }

    private static Task WaitForMovesAsync(params Player[] players)
    {
        return MetricAssertions.UntilAsync(() => players.All(e => e.Moves().Length > 0), "Every player in sight hears the move.");
    }

    private static async Task<PlayerFrame> FirstAsync(Player player, GroupOpcode group, byte command)
    {
        await MetricAssertions.UntilAsync(() => player.Received().Any(e => e.Is(group, command)), $"The player gets {group}/{command}.");

        return player.Received().First(e => e.Is(group, command));
    }
}
