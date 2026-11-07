using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Cache;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Server;
using OpenWorker.DistrictServer.Server.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Gameplay.Modules.Gestures.Components;
using OpenWorker.Hotspot.Modules.Gestures.Responses;
using OpenWorker.Hotspot.Modules.Gestures.Types;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.Domain.Enums;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM.Searching;
namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class PersonService(
    World ecs,
    BatchManager manager,
    SoulMetryManager soulMetry,
    DailyMissionManager dailyMissions,
    AttendanceManager attendance,
    BoosterManager boosters,
    HelperManager helpers,
    IRedisCollection<SessionCache> sessions,
    IRedisCollection<DistrictReserveCache> districtReserves,
    SessionLeaseManager leases,
    ServiceChannels channels,
    IDbContextFactory<PersistenceContext> factory,
    IConfiguration configuration,
    PersonRegistry registry,
    LeaveManager leaving,
    QuestManager questManager,
    ReadOnlyCollection<CharacterInfoRow> characterInfo
) :
    IHotspotHandler<PersonEnterGameServerRequest>,
    IHotspotHandler<PersonInfoReqRequest>,
    IHotspotHandler<PersonTradePasswordRequest>,
    IHotspotHandler<PersonChangeServerRequest>
{
    private short Location { get; } = configuration.GetLocation();
    private Guid Instance { get; } = configuration.GetInstance();

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonInfoReqRequest request)
    {
        // The person is always taken from the session, never from the request.
        if (ecs.Get<ClaimsComponent>(context.Player).Account != 0)
        {
            ecs.Get<ServerSessionComponent>(context.Player)
                .Send(PersonSnapshotMapper.CreateCharacterInfoResponse(ecs, context.Player));
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonEnterGameServerRequest request)
    {
        var session = ecs.Get<ServerSessionComponent>(context.Player);

        if (request.Session.Account != request.Account ||
            await sessions.AnyAsync(e => e.Session == request.Session.Key) is false)
        {
            session.Disconnect();
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        // TODO: Gate must make a request to reserve space for the player and
        //       person loading must be done before the player is connected
        //       without using Actor
        var person = await database.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Account.Id == request.Account && e.Id == request.Actor.Identifier, context.CancellationToken);

        if (person is null)
        {
            session.Disconnect();
            return;
        }

        ecs.Set(context.Player, new ClaimsComponent(request.Session));

        await leases
            .AttachAsync(context.Player, request.Session.Key, request.Session.Account);

        if (!manager.TryGetAndCache(Location, out var batch, out var type) || type != BatchType.District)
        {
            return;
        }

        ecs.Set(context.Player, await TakeReservedPlaceAsync(request) ?? GetDefaultPlace(batch));

        ecs.Set(context.Player, new KeepAliveComponent());

        ecs.Set(context.Player, new CurrencyComponent
        {
            Gold = 5_000_000,
            Cash = 7_000_000,
            BattlePoint = 10_000_000,
            Ether = 15_000_000
        });

        ecs.Set(context.Player, new GestureComponent
        {
            Collection = person.GestureList.Length switch
            {
                < GesturesModuleDefines.MaxGestureCount => person.GestureList
                    .Concat(Enumerable
                        .Repeat(0, GesturesModuleDefines.MaxGestureCount - person.GestureList.Length)
                        .ToArray())
                    .ToArray(),

                > GesturesModuleDefines.MaxGestureCount => person.GestureList.Take(GesturesModuleDefines.MaxGestureCount).ToArray(),

                _ => person.GestureList
            }
        });

        registry.PullPerson(context.Player, person);

        ecs.Set(context.Player, new SkillPointComponent
        {
            TotalSkillPoint = 32,
            FreeSkillPoint = 16
        });

        ecs.Set(context.Player, new SkillLearnedComponent
        {
            Values = GetDefaultSkills(person.Hero)
        });

        ecs.Set(context.Player, new SkillDeckComponent
        {
            SlotList = [],
            SlotCount = 4
        });

        ecs.Set(context.Player, new SkillDeckBonusComponent
        {
            Values = [1, 2, 3, 4]
        });

        session.Send(PersonSnapshotMapper.CreateCharacterInfoResponse(ecs, context.Player));

        session.Send(PersonProfileServiceBase.CreateCommunityResponse(ecs.Get<CommunityComponent>(context.Player)));

        var actor = ecs.Get<ActorComponent>(context.Player);

        session.Send(new GestureSlotLoadResponse { Gestures = ecs.Get<GestureComponent>(context.Player).Collection });
        session.Send(new PersonUpdateOriginStatListResponse { Actor = actor });
        await helpers.EnterAsync(context.Player, person.Id, context.CancellationToken);
        await boosters.EnterAsync(context.Player, person.Id, BoosterConsumeArea.District, context.CancellationToken);

        await soulMetry.EnterAsync(context.Player, person.Id, context.CancellationToken);
        await dailyMissions.EnterAsync(context.Player, person.Id, context.CancellationToken);

        await questManager.EnterAsync(context.Player, person.Id, context.CancellationToken);

        JoinChannel(context.Player);

        await attendance.EnterAsync(context.Player, person.Id, context.CancellationToken);

        // Get skill components
        ref readonly var skillPoint = ref ecs.Get<SkillPointComponent>(context.Player);
        ref readonly var skillLearned = ref ecs.Get<SkillLearnedComponent>(context.Player);
        ref readonly var skillDeck = ref ecs.Get<SkillDeckComponent>(context.Player);
        ref readonly var skillDeckBonus = ref ecs.Get<SkillDeckBonusComponent>(context.Player);

        session.Send(new SkillLoadInfoResponse
        {
            Snapshot = new SkillSnapshotValue
            {
                Target = actor,
                SkillPoint = new SkillPointValue
                {
                    TotalSkillPoint = skillPoint.TotalSkillPoint,
                    FreeSkillPoint = skillPoint.FreeSkillPoint,
                },
                DeckSlotCount = skillDeck.SlotCount,
                DeckBonus = skillDeckBonus.Values,
                SkillList = skillLearned.Values,
                DeckSlotList = skillDeck.SlotList
            }
        });
    }

    /// <summary>
    /// The place chosen by the server the person came from, if it was reserved for this district.
    /// </summary>
    private async ValueTask<WorldComponent?> TakeReservedPlaceAsync(PersonEnterGameServerRequest request)
    {
        var reserve = await districtReserves
            .FindByIdAsync(request.Account.ToString());

        if (reserve is null)
        {
            return null;
        }

        await districtReserves
            .DeleteAsync(reserve);

        if (reserve.Person != request.Actor.Identifier || reserve.District != Instance)
        {
            return null;
        }

        return new WorldComponent
        {
            Location = Location,
            Position = new Vector3(reserve.X, reserve.Y, reserve.Z),
            Rotation = reserve.R,
            Jump = reserve.Jump
        };
    }

    private WorldComponent GetDefaultPlace(VBatchFile batch)
    {
        Debug.Assert(batch.EventBox.StartEvents.Count > 0);

        var start = batch.EventBox.StartEvents.First(x => x.Id == Location * 100 + 1);

        return new WorldComponent
        {
            Location = Location,
            Position = start.GetPosition(),
            Rotation = start.Rotation,
            Jump = start.Id
        };
    }

    private SkillInfoValue[] GetDefaultSkills(Hero hero)
    {
        var charInfo = characterInfo.FirstOrDefault(c => c.Character == (byte)hero);
        if (charInfo.Id == 0)
        {
            return [];
        }

        return charInfo
            .GetSkillList()
            .Where(id => id > 0)
            .Select(id => new SkillInfoValue { Skill = id, Divergence = 0 })
            .ToArray();
    }

    private void JoinChannel(Entity entity)
    {
        // Try to find a channel that's not full

        for (short index = 0; index < channels.Count; index++)
        {
            if (channels[index].Workload == ChannelWorkload.Full)
            {
                continue;
            }

            channels.Join(index, entity);

            return;
        }

        // If all channels are full, find the one with the least players

        {
            var (index, _) = Enumerable.Range(0, channels.Count)
                .Select(index => ((short)index, Count: channels[index].Online))
                .Aggregate((min, current) => current.Count < min.Count ? current : min);

            channels.Join(index, entity);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonChangeServerRequest request)
    {
        await leaving
            .LeaveAsync(context.Player, request.Destination);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonTradePasswordRequest request)
    {
        var session = ecs.Get<ServerSessionComponent>(context.Player);

        // session.Send(new PersonTradePasswordResponse(request.Password));

        session.Send(new CharacterTradePasswordResponse
        {
            ProtectionState = PasswordProtectionState.Authenticated,
            ErrorCode = 0
        });

        return ValueTask.CompletedTask;
    }
}
