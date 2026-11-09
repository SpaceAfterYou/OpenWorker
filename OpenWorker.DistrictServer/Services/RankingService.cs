using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.Ranking.Requests;
using OpenWorker.Hotspot.Modules.Ranking.Responses;
using OpenWorker.Hotspot.Modules.Ranking.Types;
using OpenWorker.Persistence;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The rankings, from what the servers recorded: a clear of a map and the kills in the arena. Where nothing is recorded the
/// list is empty. The infinite tower has nothing to show yet.
/// </summary>
public sealed class RankingService(World world, IDbContextFactory<PersistenceContext> factory) :
    IHotspotHandler<RankingTimeAttackListRequest>,
    IHotspotHandler<RankingPvpListRequest>,
    IHotspotHandler<RankingInfiniteTowerListRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, RankingTimeAttackListRequest request)
    {
        var me = world.Get<ActorComponent>(context.Player).Identifier;
        var (top, mine) = await factory.RankingAsync(RankingKind.TimeAttack, request.World, me);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        ST_RANKING_TIME_ATTACK_USER Entry(int person, string name, byte hero, byte level, int order, int time, int score) => new()
        {
            Person = person,
            World = request.World,
            Order = (short)order,
            Hero = (Hero)hero,
            Level = level,
            Name = name,
            Time = time,
            Score = score
        };

        var info = world.Get<PersonInfoComponent>(context.Player);

        world.Get<ServerSessionComponent>(context.Player).Send(new RankingTimeAttackListResponse
        {
            Data = new ST_RANKING_TIME_ATTACK_USER2
            {
                Player = Entry(me, info.Name, (byte)info.Hero, Level(context.Player), mine?.Order ?? 0, mine?.Time ?? 0, mine?.Score ?? 0),
                ParticipantList = new ST_RANKING_TIME_ATTACK_USER3
                {
                    World = request.World,
                    aUserList = top.Select(e => Entry(e.Person, e.Name, e.Hero, e.Level, e.Order, e.Time, e.Score)).ToArray()
                }
            }
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, RankingPvpListRequest request)
    {
        var me = world.Get<ActorComponent>(context.Player).Identifier;
        var (top, mine) = await factory.RankingAsync(RankingKind.Pvp, (int)request.Type, me);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        ST_USER_RANKING_INFO Entry(int person, string name, byte hero, byte level, int order, int score) => new()
        {
            Person = person,
            Order = (short)order,
            Hero = (Hero)hero,
            Level = level,
            Name = name,
            KillScore = score
        };

        var info = world.Get<PersonInfoComponent>(context.Player);

        world.Get<ServerSessionComponent>(context.Player).Send(new RankingPvpListResponse
        {
            Data = new PS_RANKING_LIST_RES
            {
                Type = request.Type,
                Player = Entry(me, info.Name, (byte)info.Hero, Level(context.Player), mine?.Order ?? 0, mine?.Score ?? 0),
                ParticipantList = top.Select(e => Entry(e.Person, e.Name, e.Hero, e.Level, e.Order, e.Score)).ToArray()
            }
        });
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, RankingInfiniteTowerListRequest request)
    {
        world.Get<ServerSessionComponent>(context.Player).Send(RankingInfiniteTowerListResponse.Create(request.Chapter));

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The level of the player who asks; the others' come from their rows.
    /// </summary>
    private byte Level(Entity player) => world.TryGet<LevelComponent>(player, out var level) ? level.Level : (byte)1;
}
