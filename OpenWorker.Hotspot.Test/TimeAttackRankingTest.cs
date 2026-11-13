using System.Buffers.Binary;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Ranking;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class TimeAttackRankingTest
{
    private const int Person = 41;
    private const int Map = 21011;

    /// <summary>
    /// The client asks for a map's list once per login: a clear that sets the player's time sends it the list with its new row,
    /// a clear that sets nothing (a slower time, a maze the window does not list) sends nothing.
    /// </summary>
    [TestMethod]
    public async Task OnlyAClearThatRecordsATimeSendsThePlayerTheList()
    {
        using var world = World.Create();
        var connection = new RecordingConnection();
        var store = new Store();
        var ranking = new TimeAttackRanking(world, store);

        var player = world.Create(new ActorComponent(Person, ActorType.User), new PersonInfoComponent { Name = "Me", Hero = Hero.Haru },
            new ServerSessionComponent(connection));

        store.Better = false;
        await ranking.RecordAsync(player, Map, mazeType: 2, seconds: 400, score: 10);

        store.Better = true;
        await ranking.RecordAsync(player, 10101, mazeType: 1, seconds: 300, score: 10);
        await ranking.RecordAsync(player, Map, mazeType: 8, seconds: 300, score: 20);

        var until = DateTime.UtcNow.AddSeconds(5);

        while (connection.Frames.Length == 0 && DateTime.UtcNow < until)
        {
            await Task.Delay(10);
        }

        var frames = connection.Frames.Select(PlayerFrame.Open).ToArray();

        Assert.HasCount(2, store.Submitted, "a maze the window does not list is not recorded");
        Assert.HasCount(1, frames);
        Assert.IsTrue(frames[0].Is(GroupOpcode.Ranking, (byte)RankingOpcode.TimeAttackList));

        var body = frames[0].Body.AsSpan();

        Assert.AreEqual(Person, BinaryPrimitives.ReadInt32LittleEndian(body), "the player's own row");
        Assert.AreEqual(Map, BinaryPrimitives.ReadInt32LittleEndian(body[4..]));
        Assert.AreEqual(1, BinaryPrimitives.ReadInt16LittleEndian(body[8..]), "its new place");
    }

    private sealed class Store : IRankingStore
    {
        private RankingRow? _row;

        public bool Better { get; set; }

        public List<int> Submitted { get; } = [];

        public Task<bool> SubmitAsync(RankingKind kind, int scope, int person, int time, int score, CancellationToken cancellationToken = default)
        {
            Submitted.Add(scope);

            if (Better)
            {
                _row = new RankingRow(1, person, "Me", (byte)Hero.Haru, time, score);
            }

            return Task.FromResult(Better);
        }

        public Task<(IReadOnlyList<RankingRow> Top, RankingRow? Mine)> RankingAsync(RankingKind kind, int scope, int person, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RankingRow> top = _row is null ? [] : [_row];

            return Task.FromResult((top, _row));
        }
    }
}
