using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Lua;
using Lua.Standard;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Quests.Responses;
using OpenWorker.Hotspot.Modules.Quests.Types;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.SoulMetry.Responses;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;
using OpenWorker.Lua;
using OpenWorker.Lua.Managers;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class PersonService(
    World world,
    ReadOnlyCollection<TitleInfoRow> titleCollection,
    ReadOnlyCollection<SoulMetryRow> soulMetryCollection,
    ReadOnlyCollection<QuestEpisodeRow> questEpisodeCollection,
    ReadOnlyCollection<MazeInfoRow> mazeInfoCollection,
    IRedisCollection<SessionCache> sessions,
    BatchManager batches,
    IRedisCollection<MazeReserveCache> mazeReserves,
    IDbContextFactory<PersistenceContext> factory,
    PersonRegistry registry,
    BuffManager buffManager,
    IConfiguration configuration
) :
    IHotspotHandler<PersonEnterGameServerRequest>,
    IHotspotHandler<PersonLoadTitleRequest>,
    IHotspotHandler<PersonTradePasswordRequest>
{
    private short Gate { get; } = configuration.GetGate();
    
    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonEnterGameServerRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());
        
        if (!await sessions.AnyAsync(e => e.Session == request.Session.Key).ConfigureAwait(false))
        {
            session.Disconnect();
            return;
        }
        
        var mazeReserve = await mazeReserves
            .FirstAsync(e => e.Person == request.Actor.Identifier)
            .ConfigureAwait(false);

        if (!batches.TryGetAndCache(mazeReserve.Location, out var batch, out var type) || type != BatchType.Maze)
        {
            return;
        }
        
        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var person = await database.Persons
            .AsNoTracking()
            .FirstAsync(e => e.Account.Id == request.Account && e.Id == request.Actor.Identifier, context.CancellationToken)
            .ConfigureAwait(false);
        
        world.Set(context.GetPlayerEntity(), new ClaimsComponent(request.Session));
        
        world.Set(context.GetPlayerEntity(), new WorldComponent
        {
            Location = mazeReserve.Location,
            Position = new Vector3(mazeReserve.X, mazeReserve.Y, mazeReserve.Z),
            Rotation = mazeReserve.R,
            Jump = mazeReserve.Jump,
            Map = new MapValue
            {
                Location = mazeReserve.Location,
                Server = Gate
            }
        });

        world.Set(context.GetPlayerEntity(), new CurrencyComponent
        {
            Gold = 5_000_000,
            Cash = 7_000_000,
            BattlePoint = 10_000_000,
            Ether = 15_000_000
        });
        
        registry.PullPerson(context.GetPlayerEntity(), person);
        
        var mazeInfo = mazeInfoCollection.First(e => e.Id == mazeReserve.Location);
        
        var state = LuaState.Create();
        state.OpenStandardLibraries();
        
        var table = new LuaTable
        {
            ["GetHelper"] = new LuaFunction((ctx, _) =>
            {
                ctx.Return(new LuaGameHelper());
                return ValueTask.FromResult(1);
            })
        };

        state.Environment["Soulworker"] = new LuaValue(table);
        

        await state
            .DoFileAsync(Path.Join("scripts", $"{mazeInfo.Script}.lua"), context.CancellationToken)
            .ConfigureAwait(false);
        
        var creatureManager = new LuaCreatureManager(world, batch);

        creatureManager.Emplace(context.GetPlayerEntity());

        var maze = new LuaMaze(state, world, context.GetPlayerEntity(), new LuaCreatureManager(world, batch), buffManager, batch);
        
        world.Add(context.GetPlayerEntity(), maze);
        
        session.Send(new CharacterInfoResponse
        {
            Person = PersonSnapshotMapper.CreatePersonValue(world, context.GetPlayerEntity(), context.GetPlayerEntity()),
            World = PersonSnapshotMapper.CreateWorldValue(world, context.GetPlayerEntity()),
            Gate = PersonSnapshotMapper.CreateCharacterInfoGatePayload(world, context.GetPlayerEntity())
        });
        
        await state
            .OnEnterPlayerAsync(world.Get<ActorComponent>(context.GetPlayerEntity()), maze)
            .ConfigureAwait(false);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonLoadTitleRequest request)
    {
        var list = titleCollection
            .Where(e => e is { Field2: 0, Field3: 0 })
            .Select(e => e.Id)
            .ToArray();

        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());
        
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

    private void SendQuestEpisodeList(ServerSessionComponent session)
    {
        var list = questEpisodeCollection
            .Take(1)
            .Select(e => new QuestEpisodeEntry
            {
                Index = e.Id,
                Info = new QuestInfoEntry
                {
                    AddHelper = 0,
                    CompleteBit = 0,
                    Failed = false,
                    Condition =
                    [
                        new QuestCondition
                        {
                            Condition = e.Field96,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field97,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field98,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field99,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field100,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field101,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field102,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field103,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field104,
                            Step = 0
                        },
                        new QuestCondition
                        {
                            Condition = e.Field105,
                            Step = 0
                        }
                    ]
                }
            })
            .ToArray();

        session.Send(new QuestListResponse { List = list });
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonTradePasswordRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());
        
        // session.Send(new PersonTradePasswordResponse(request.Password));
        
        session.Send(new CharacterTradePasswordResponse
        {
            State = E_PASSWORD_STATE.ePASSWORD_STATE_AUTHENTICATED,
            ErrorCode = 0
        });
        
        return ValueTask.CompletedTask;
    }
}