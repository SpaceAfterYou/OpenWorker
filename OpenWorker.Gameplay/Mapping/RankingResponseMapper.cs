using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Ranking.Responses;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Gameplay.Mapping;

public static class RankingResponseMapper
{
    public static RankingTimeAttackListResponse CreateTimeAttackList(World world, Entity player, int targetWorld)
    {
        var data = new ST_RANKING_TIME_ATTACK_USER2
        {
            Player = new ST_RANKING_TIME_ATTACK_USER
            {
                Person = world.Get<ActorComponent>(player).Identifier,
                World = targetWorld,
                Order = 22,
                Hero = world.Get<PersonInfoComponent>(player).Hero,
                Level = 1,
                Name = world.Get<PersonInfoComponent>(player).Name,
                Time = 1_000,
                Score = 10_000
            },
            ParticipantList = new ST_RANKING_TIME_ATTACK_USER3
            {
                World = targetWorld,
                aUserList = Enumerable.Range(0, 50).Select((x, i) => new ST_RANKING_TIME_ATTACK_USER
                {
                    Person = (int)(10_000 + i),
                    World = targetWorld,
                    Order = (short)(1 + i),
                    Hero = (Hero)Random.Shared.Next(1, 7),
                    Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                    Name = $"rank. test name {i}",
                    Time = 10 + i,
                    Score = 100 + i
                }).ToArray()
            }
        };

        return new RankingTimeAttackListResponse { Data = data };
    }

    public static RankingPvpListResponse CreatePvpList(World world, Entity player, RankingType type)
    {
        var data = new PS_RANKING_LIST_RES
        {
            Type = type,
            Player = new ST_USER_RANKING_INFO
            {
                Person = world.Get<ActorComponent>(player).Identifier,
                Order = 1,
                Hero = world.Get<PersonInfoComponent>(player).Hero,
                Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                Name = world.Get<PersonInfoComponent>(player).Name,
                KillScore = (int)Random.Shared.Next(1, short.MaxValue)
            },
            ParticipantList = CreateRandomUserRankingInfo().ToArray()
        };

        return new RankingPvpListResponse { Data = data };
    }

    private static IEnumerable<ST_USER_RANKING_INFO> CreateRandomUserRankingInfo()
    {
        for (short i = 0; i < 3; i++)
        {
            yield return new ST_USER_RANKING_INFO
            {
                Person = (int)(10_000 + i),
                Order = (short)(1 + i),
                Hero = (Hero)Random.Shared.Next(1, 7),
                Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                Name = $"cool girl {1 + i}",
                KillScore = (int)Random.Shared.Next(1, short.MaxValue)
            };
        }
    }
}
