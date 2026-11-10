using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Server.Common;
using OpenWorker.Hotspot.Cache;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.DistrictServer.Server;
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
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.Domain.Enums;
using OpenWorker.Persistence;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Skill;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM.Searching;
namespace OpenWorker.DistrictServer.Services;

public sealed partial class PersonService(
    World ecs,
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
    IOptions<InstanceOptions> instance,
    PersonRegistry registry,
    InventoryPersistence inventories,
    PersonSkills skills,
    PlayerStatsService stats,
    LeaveManager leaving,
    QuestManager questManager,
    CommunityNetwork community,
    TimeProvider time,
    AchievementManager achievements,
    SharePointManager sharePoints,
    NpcCreditManager npcCredit,
    ILogger<PersonService> logger
) :
    IHotspotHandler<PersonEnterGameServerRequest>,
    IHotspotHandler<PersonInfoReqRequest>,
    IHotspotHandler<PersonChangeServerRequest>
{
    private short Location { get; } = instance.Value.Location;
    private Guid Instance { get; } = instance.Value.Identifier;

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

        // Every way into a district writes a reservation first (the gate, a portal or a warp of
        // another district, the exit of a maze), so a connection without one did not come that way.
        var reserve = await districtReserves
            .FindByIdAsync(request.Account.ToString(CultureInfo.InvariantCulture));

        if (reserve is null)
        {
            LogNoReservation(request.Account, request.Actor.Identifier, Location);
            session.Disconnect();
            return;
        }

        // The reservation of another person or another district stays: its owner still has to come.
        if (!reserve.Admits(request.Actor.Identifier, Instance))
        {
            LogForeignReservation(request.Account, request.Actor.Identifier, reserve.Person, reserve.District);
            session.Disconnect();
            return;
        }

        var entry = await factory
            .LoadPersonAtEntryAsync(request.Account, request.Actor.Identifier, context.CancellationToken);

        if (entry is null)
        {
            session.Disconnect();
            return;
        }

        var person = entry.Person;

        // A reservation is used up by the entry.
        await districtReserves
            .DeleteAsync(reserve);

        ecs.Set(context.Player, new ClaimsComponent(request.Session));

        ecs.SetLogAccount(context.Player, request.Session.Account);

        await leases
            .AttachAsync(context.Player, request.Session.Key, request.Session.Account);

        ecs.Set(context.Player, GetReservedPlace(reserve));

        await inventories.LoadAsync(context.Player, person, context.CancellationToken);

        registry.PullPerson(context.Player, entry);

        await skills.LoadAsync(context.Player, person.Id, person.Hero, context.CancellationToken);

        session.Send(PersonSnapshotMapper.CreateCharacterInfoResponse(ecs, context.Player));

        // The date of the server: the game scene counts the times left on items, boosters and windows from it.
        session.Send(new WorldCurDateResponse { Date = time.GetUtcNow() });

        session.Send(PersonProfileServiceBase.CreateCommunityResponse(ecs.Get<CommunityComponent>(context.Player)));

        var actor = ecs.Get<ActorComponent>(context.Player);

        session.Send(new GestureSlotLoadResponse { Gestures = ecs.Get<GestureComponent>(context.Player).Collection });
        stats.SendAll(context.Player);
        await helpers.EnterAsync(context.Player, person.Id, context.CancellationToken);
        await boosters.EnterAsync(context.Player, person.Id, BoosterConsumeArea.District, context.CancellationToken);

        await soulMetry.EnterAsync(context.Player, person.Id, context.CancellationToken);
        await dailyMissions.EnterAsync(context.Player, person.Id, context.CancellationToken);

        await questManager.EnterAsync(context.Player, person.Id, context.CancellationToken);

        JoinChannel(context.Player);

        await attendance.EnterAsync(context.Player, person.Id, context.CancellationToken);

        await achievements.EnterAsync(context.Player, person.Id, context.CancellationToken);
        await sharePoints.EnterDistrictAsync(context.Player, person.Id, Location, context.CancellationToken);
        await npcCredit.EnterAsync(context.Player, person.Id, context.CancellationToken);

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

        await community.EnterAsync(context.Player);
    }

    /// <summary>
    /// The place chosen by the server the person came from.
    /// </summary>
    private WorldComponent GetReservedPlace(DistrictReserveCache reserve)
    {
        return new WorldComponent
        {
            Location = Location,
            Position = new Vector3(reserve.X, reserve.Y, reserve.Z),
            Rotation = reserve.R,
            Jump = reserve.Jump
        };
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} came to district {Location} as person {Person} with no reservation, or with one that has expired, and is turned away")]
    private partial void LogNoReservation(int account, int person, short location);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} came as person {Person}, but its reservation is for person {ReservedPerson} in district {ReservedDistrict}, and it is turned away")]
    private partial void LogForeignReservation(int account, int person, int reservedPerson, Guid reservedDistrict);
}
