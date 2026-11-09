using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Channel.Community;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Lua;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A party enters a maze together: every member is asked, all who agreed go into one instance, a no ends it, a later member
/// joins the instance, and a player without a party enters as before.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PartyMazeEntryTest
{
    private sealed class Party(int id, params int[] members) : IPartyRoster
    {
        public ValueTask<IReadOnlyList<int>> MembersAsync(int person) =>
            ValueTask.FromResult<IReadOnlyList<int>>(members.Contains(person) ? members : []);

        public ValueTask<PartyView?> PartyAsync(int person) =>
            ValueTask.FromResult(members.Contains(person) ? new PartyView(id, members[0], members) : null);
    }

    private sealed class Routes : IPartyMazeRoutes
    {
        public int Opened { get; private set; }
        public List<(Entity Player, Guid Instance)> Sent { get; } = [];
        public PartyMazeCache? Running { get; set; }

        public bool IsMaze(short location) => true;

        public ValueTask<PartyMazeCache?> FindAsync(int party) => ValueTask.FromResult(Running);

        public ValueTask<PartyMazeCache> OpenAsync(int party, short location)
        {
            Opened++;

            Running = new PartyMazeCache { Party = party, Location = location, Instance = Guid.NewGuid(), Server = Guid.NewGuid(), Host = "maze", Port = 1 };

            return ValueTask.FromResult(Running);
        }

        public ValueTask<bool> SendAsync(Entity player, short location, int jump, PartyMazeCache maze)
        {
            Sent.Add((player, maze.Instance));

            return ValueTask.FromResult(true);
        }
    }

    private static readonly MessageOpcode Enter = MessageOpcode.From(GroupOpcode.World, WorldOpcode.EnterReq);

    private static MazeEnterPartyAnswerRequest Answer(short maze, bool agree)
    {
        var body = new byte[5];

        BitConverter.TryWriteBytes(body, (int)maze);
        body[4] = agree ? (byte)1 : (byte)0;

        var reader = new Wire.PacketReader(body);

        return MazeEnterPartyAnswerRequest.Read(ref reader);
    }

    /// <summary>
    /// The master takes the portal: every member is asked, and when all have said yes one instance is opened and every
    /// member is sent into that one. A member who comes later goes into it without a question.
    /// </summary>
    [TestMethod]
    public void APartyReservationSendsEveryAgreeingMemberToOneInstance()
    {
        const short maze = 21311;

        var community = new MemoryCommunity();
        using var server = new TestServer(community);
        using var master = server.Join(1, "Master");
        using var member = server.Join(2, "Member");
        using var late = server.Join(3, "Late");

        foreach (var player in new[] { master, member })
        {
            server.World.Add(player.Entity, new ChannelMemberComponent(1));
        }

        var routes = new Routes();
        var entry = new PartyMazeEntry(server.World, new Party(10, 1, 2), server.Network, routes, NullLogger<PartyMazeEntry>.Instance);
        var handled = false;

        server.Run(async () => handled = await entry.TryEnterAsync(master.Entity, maze, 0, Enter));

        Assert.IsTrue(handled);
        Assert.AreEqual(0, routes.Opened, "Nobody goes before everybody has said yes");

        foreach (var player in new[] { master, member })
        {
            Assert.IsTrue(player.Read().Any(e => e.Group == (byte)GroupOpcode.Party && e.Command == (byte)PartyOpcode.Unknown5), "Every member, the master too, is asked");
        }

        server.Run(() => entry.AnswerAsync(member.Entity, Answer(maze, true)).AsTask());

        Assert.AreEqual(1, routes.Opened, "One instance for the party");
        CollectionAssert.AreEquivalent(new[] { master.Entity, member.Entity }, routes.Sent.Select(e => e.Player).ToArray());
        Assert.AreEqual(1, routes.Sent.Select(e => e.Instance).Distinct().Count(), "Every member goes into the same instance");

        // A member who was not there takes the portal later, from wherever, and goes where the party is.
        var lateParty = new PartyMazeEntry(server.World, new Party(10, 1, 3), server.Network, routes, NullLogger<PartyMazeEntry>.Instance);

        server.Run(async () => handled = await lateParty.TryEnterAsync(late.Entity, maze, 0, Enter));

        Assert.IsTrue(handled);
        Assert.AreEqual(1, routes.Opened);
        Assert.AreEqual(routes.Running!.Instance, routes.Sent[^1].Instance);
        Assert.AreEqual(late.Entity, routes.Sent[^1].Player);
    }

    /// <summary>
    /// A member who says no ends the question for everybody, and nobody goes.
    /// </summary>
    [TestMethod]
    public void AMemberWhoSaysNoEndsTheQuestionForEverybody()
    {
        const short maze = 21311;

        var community = new MemoryCommunity();
        using var server = new TestServer(community);
        using var master = server.Join(1, "Master");
        using var member = server.Join(2, "Member");

        foreach (var player in new[] { master, member })
        {
            server.World.Add(player.Entity, new ChannelMemberComponent(1));
        }

        var routes = new Routes();
        var entry = new PartyMazeEntry(server.World, new Party(10, 1, 2), server.Network, routes, NullLogger<PartyMazeEntry>.Instance);

        server.Run(() => entry.TryEnterAsync(master.Entity, maze, 0, Enter).AsTask());
        server.Run(() => entry.AnswerAsync(member.Entity, Answer(maze, false)).AsTask());

        foreach (var player in new[] { master, member })
        {
            Assert.IsTrue(player.Read().Any(e => e.Group == (byte)GroupOpcode.Maze && e.Command == (byte)MazeOpcode.eSUB_CMD_MAZE_CANCEL_ENTER_PARTY), "Everybody hears that it is off");
        }

        Assert.AreEqual(0, routes.Opened);
        Assert.IsEmpty(routes.Sent);
        Assert.IsFalse(entry.IsAsked(1));
        Assert.IsFalse(entry.IsAsked(2));
    }

    /// <summary>
    /// A player who is in no party is left to the entry of a player alone, as before.
    /// </summary>
    [TestMethod]
    public void APlayerWithoutAPartyEntersAsBefore()
    {
        var community = new MemoryCommunity();
        using var server = new TestServer(community);
        using var alone = server.Join(5, "Alone");

        var routes = new Routes();
        var entry = new PartyMazeEntry(server.World, new Party(10, 1, 2), server.Network, routes, NullLogger<PartyMazeEntry>.Instance);
        var handled = true;

        server.Run(async () => handled = await entry.TryEnterAsync(alone.Entity, 21311, 0, Enter));

        Assert.IsFalse(handled, "The entry of a player alone takes over");
        Assert.AreEqual(0, routes.Opened);
        Assert.IsEmpty(alone.Read(100));
    }
}
