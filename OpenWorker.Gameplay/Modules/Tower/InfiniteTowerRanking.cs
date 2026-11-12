using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Stores;
using OpenWorker.Hotspot.Modules.Ranking.Responses;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Gameplay.Modules.Tower;

/// <summary>
/// The tower in the ranking window of the client (2C/03), from the clears the stages recorded: the person and its friends,
/// never the whole server.
/// <list type="bullet">
/// <item>For each chapter, how many friends have cleared it: they have a clear of its last stage.</item>
/// <item>For each stage of the chapter asked for, the person's own time and the fastest clear among the person and its
/// friends (of two equal times the one recorded first).</item>
/// </list>
/// </summary>
public static class InfiniteTowerRanking
{
    public static RankingInfiniteTowerClearInfo Build(InfiniteTower tower, int person, int chapter, IReadOnlyCollection<TowerClear> clears)
    {
        var byMaze = clears.ToLookup(e => e.Maze);
        var friendsCleared = new int[RankingInfiniteTowerListResponse.Chapters];

        for (var index = 0; index < friendsCleared.Length; index++)
        {
            if (tower.MazesOf(index + 1) is [.., var last])
            {
                friendsCleared[index] = byMaze[last].Where(e => e.Person != person).Select(e => e.Person).Distinct().Count();
            }
        }

        var mazes = tower.MazesOf(chapter);
        var stages = new RankingInfiniteTowerRankingInfo[mazes.Count];

        for (var index = 0; index < mazes.Count; index++)
        {
            var cleared = byMaze[mazes[index]].ToList();
            var mine = cleared.FirstOrDefault(e => e.Person == person);
            var best = cleared.OrderBy(e => e.Time).ThenBy(e => e.Order).FirstOrDefault();

            stages[index] = best is null
                ? RankingInfiniteTowerListResponse.Empty with { MyClearTime = mine?.Time ?? 0 }
                : new RankingInfiniteTowerRankingInfo
                {
                    MyClearTime = mine?.Time ?? 0,
                    Chapter = chapter,
                    Stage = index + 1,
                    Person = best.Person,
                    Hero = (Hero)best.Hero,
                    Level = best.Level,
                    Name = best.Name,
                    ClearTime = best.Time
                };
        }

        return new RankingInfiniteTowerClearInfo { FriendsClearedPerChapter = friendsCleared, Stages = stages, Chapter = chapter };
    }
}
