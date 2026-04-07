using System.Collections.ObjectModel;
using System.Diagnostics;
using Arch.Core;
using OpenWorker.Domain.Components;
using Microsoft.EntityFrameworkCore;
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
using OpenWorker.Hotspot.Modules.Events.Enums;
using OpenWorker.Hotspot.Modules.Events.Responses;
using OpenWorker.Hotspot.Modules.Events.Types;
using OpenWorker.Gameplay.Modules.Gestures.Components;
using OpenWorker.Hotspot.Modules.Gestures.Responses;
using OpenWorker.Hotspot.Modules.Gestures.Types;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.SoulMetry.Responses;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM.Searching;
namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class PersonService(
    World ecs,
    BatchManager manager,
    ReadOnlyCollection<TitleInfoRow> titleCollection,
    ReadOnlyCollection<SoulMetryRow> soulMetryCollection,
    IRedisCollection<SessionCache> sessions,
    ServiceChannels channels,
    IDbContextFactory<PersistenceContext> factory,
    IConfiguration configuration,
    PersonRegistry registry,
    QuestManager questManager
) :
    IHotspotHandler<PersonEnterGameServerRequest>,
    IHotspotHandler<PersonLoadTitleRequest>,
    IHotspotHandler<PersonTradePasswordRequest>
{
    private short Location { get; } = configuration.GetLocation();

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonEnterGameServerRequest request)
    {
        var session = ecs.Get<ServerSessionComponent>(context.Player);

        if (await sessions.AnyAsync(e => e.Session == request.Session.Key).ConfigureAwait(false) is false)
        {
            session.Disconnect();
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        // TODO: Gate must make a request to reserve space for the player and
        //       person loading must be done before the player is connected
        //       without using Actor
        var person = await database.Persons
            .AsNoTracking()
            .FirstAsync(e => e.Account.Id == request.Account && e.Id == request.Actor.Identifier, context.CancellationToken)
            .ConfigureAwait(false);

        ecs.Set(context.Player, new ClaimsComponent(request.Session));

        if (!manager.TryGetAndCache(Location, out var batch, out var type) || type != BatchType.District)
        {
            return;
        }

        Debug.Assert(batch.EventBox.StartEvents.Count > 0);
        var start = batch.EventBox.StartEvents.First(x => x.Id == Location * 100 + 1);

        ecs.Set(context.Player, new WorldComponent
        {
            Location = Location,
            Position = start.GetPosition(),
            Rotation = start.Rotation,
            Jump = start.Id
        });

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

        session.Send(new CharacterInfoResponse
        {
            Person = PersonSnapshotMapper.CreatePersonValue(ecs, context.Player, context.Player),
            World = PersonSnapshotMapper.CreateWorldValue(ecs, context.Player),
            Gate = PersonSnapshotMapper.CreateCharacterInfoGatePayload(ecs, context.Player)
        });
        session.Send(new GestureSlotLoadResponse { Gestures = ecs.Get<GestureComponent>(context.Player).Collection });
        session.Send(new PersonUpdateOriginStatListResponse { Actor = ecs.Get<ActorComponent>(context.Player) });
        // session.Send(new BoosterLoadReply([], BoosterConsumeArea.None));

        SendSoulMetryList(session);

        questManager.SendQuestEpisodeList(context.Player);

        JoinChannel(context.Player);

        session.Send(new EventAttendanceLoadResponse
        {
            Info = new AttendanceInfo
            {
                Table = 1001,
                State = Enumerable
                    .Range(0, 40)
                    .Select(_ => AttendanceState.Available)
                    .ToArray(),
                CurrentDay = 1
            },

            Time = new AttendancePlayTime
            {
                Step = 0,
                Played = TimeSpan.Zero
            }
        });
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

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonLoadTitleRequest request)
    {
        var list = titleCollection
            .Where(e => e is { Field2: 0, Field3: 0 })
            .Select(e => e.Id)
            .ToArray();

        var session = ecs.Get<ServerSessionComponent>(context.Player);

        session.Send(new PersonLoadTitleResponse { TitleList = list, OpenList = [], Result = true });

        return ValueTask.CompletedTask;
    }

    private void SendSoulMetryList(ServerSessionComponent session)
    {
        var list = soulMetryCollection
            .Select(e => new SoulMetryValue(e.Id, 0))
            .ToArray();

        session.Send(new SoulMetryListResponse { Values = list });
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
