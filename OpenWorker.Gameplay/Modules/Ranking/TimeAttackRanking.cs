using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Ranking.Responses;
using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Gameplay.Modules.Ranking;

/// <summary>
/// The time attack list of a map (2C/01): what the ranking window asks for, and what a player is sent on its own when its
/// clear set a new best time. The client asks for a map's list once per login, so without that its new time would show
/// only after the next login; it files a list it did not ask for and redraws the page if it is open.
/// </summary>
public sealed class TimeAttackRanking(World world, IRankingStore store)
{
    // The kinds of maze the ranking window lists: the casual ones and the raids of a district.
    private const byte CasualMaze = 2;
    private const byte DistrictRaid = 8;

    /// <summary>
    /// Whether the client keeps a time attack list for a maze of this kind.
    /// </summary>
    public static bool IsRanked(byte mazeType) => mazeType is CasualMaze or DistrictRaid;

    /// <summary>
    /// Records the clear of a maze the window lists and, when it is the player's first or better time, sends the player the
    /// map's list with its new row.
    /// </summary>
    public async ValueTask RecordAsync(Entity player, int map, byte mazeType, int seconds, int score)
    {
        if (!IsRanked(mazeType) || !world.IsAlive(player) || !world.TryGet(player, out ActorComponent actor) || actor.Identifier == 0)
        {
            return;
        }

        if (!await store.SubmitAsync(RankingKind.TimeAttack, map, actor.Identifier, seconds, score))
        {
            return;
        }

        if (world.IsAlive(player))
        {
            await SendAsync(player, map);
        }
    }

    /// <summary>
    /// Sends the player the list of the map: the best clears and its own row (place 0 when it has none).
    /// </summary>
    public async ValueTask SendAsync(Entity player, int map)
    {
        var me = world.Get<ActorComponent>(player).Identifier;
        var (top, mine) = await store.RankingAsync(RankingKind.TimeAttack, map, me);

        if (!world.IsAlive(player) || !world.TryGet(player, out ServerSessionComponent session))
        {
            return;
        }

        ST_RANKING_TIME_ATTACK_USER Entry(int person, string name, byte hero, byte level, int order, int time, int score) => new()
        {
            Person = person,
            World = map,
            Order = (short)order,
            Hero = (Hero)hero,
            Level = level,
            Name = name,
            Time = time,
            Score = score
        };

        var info = world.Get<PersonInfoComponent>(player);
        var level = world.TryGet<LevelComponent>(player, out var own) ? own.Level : (byte)1;

        session.Send(new RankingTimeAttackListResponse
        {
            Data = new ST_RANKING_TIME_ATTACK_USER2
            {
                Player = Entry(me, info.Name, (byte)info.Hero, level, mine?.Order ?? 0, mine?.Time ?? 0, mine?.Score ?? 0),
                ParticipantList = new ST_RANKING_TIME_ATTACK_USER3
                {
                    World = map,
                    aUserList = top.Select(e => Entry(e.Person, e.Name, e.Hero, e.Level, e.Order, e.Time, e.Score)).ToArray()
                }
            }
        });
    }
}
