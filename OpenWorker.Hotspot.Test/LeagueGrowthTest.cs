using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.League;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a member brings to the league from a maze: half its fatigue on the way in, the other half and a hundredth of the gold
/// on the way out. The maze is made here.
/// </summary>
[TestClass]
public sealed class LeagueGrowthTest
{
    private const short Maze = 21311;
    private const short Fatigue = 14;

    private sealed class Treasury : ILeagueTreasury
    {
        public List<LeagueWealth> Shares { get; } = [];

        public void Add(LeagueWealth wealth) => Shares.Add(wealth);

        public void LevelReached(int league, int person, byte level)
        {
        }
    }

    private static MazeInfoRow MazeRow(short id, short fatigue)
    {
        // The id, the type, the group, two bytes and the fatigue; every column after them reads as zero or empty.
        var bytes = new byte[4096];

        BitConverter.TryWriteBytes(bytes, id);
        BitConverter.TryWriteBytes(bytes.AsSpan(7), fatigue);

        return new MazeInfoRow(new BinaryReader(new MemoryStream(bytes)));
    }

    [TestMethod]
    public void AMazeRunBringsHalfItsFatigueEachWayAndAHundredthOfItsGold()
    {
        using var world = World.Create();

        var treasury = new Treasury();
        var growth = new LeagueGrowth(world, new ReadOnlyCollection<MazeInfoRow>([MazeRow(Maze, Fatigue)]), treasury);

        var member = world.Create(
            new ActorComponent { Identifier = 7 },
            new LeagueComponent { Id = 3, Name = "Ab", Card = new LeagueComponentCard { Emblem = 0, Border = 0 } });

        // A player whose league the server has not learnt yet brings it all the same, for the treasury to find.
        var newcomer = world.Create(
            new ActorComponent { Identifier = 9 },
            new LeagueComponent { Id = 0, Name = string.Empty, Card = new LeagueComponentCard { Emblem = 0, Border = 0 } });

        growth.OnMazeEntered(member, Maze);
        growth.OnMazeFinished(member, Maze, 1250);

        // A maze that costs no fatigue brings nothing.
        growth.OnMazeEntered(member, 1);
        growth.OnMazeEntered(newcomer, Maze);

        CollectionAssert.AreEqual(
            new[] { new LeagueWealth(3, 7, Fatigue / 2, 0), new LeagueWealth(3, 7, Fatigue / 2, 12), new LeagueWealth(0, 9, Fatigue / 2, 0) },
            treasury.Shares);
    }
}
