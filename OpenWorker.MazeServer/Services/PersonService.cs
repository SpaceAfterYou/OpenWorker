using OpenWorker.Hotspot.Cache;
using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Lua;
using Lua.Standard;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
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
    ReadOnlyCollection<MazeInfoRow> mazeInfoCollection,
    ReadOnlyCollection<InteractionObjectRow> interactionCollection,
    IRedisCollection<SessionCache> sessions,
    SessionLeaseManager leases,
    BatchManager batches,
    IRedisCollection<MazeReserveCache> mazeReserves,
    IDbContextFactory<PersistenceContext> factory,
    PersonRegistry registry,
    LeaveManager leaving,
    BuffManager buffManager,
    QuestManager questManager,
    IConfiguration configuration
) :
    IHotspotHandler<PersonEnterGameServerRequest>,
    IHotspotHandler<PersonInfoReqRequest>,
    IHotspotHandler<PersonLoadTitleRequest>,
    IHotspotHandler<PersonTradePasswordRequest>,
    IHotspotHandler<PersonChangeServerRequest>
{
    private short Gate { get; } = configuration.GetGate();

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonInfoReqRequest request)
    {
        // The person is always taken from the session, never from the request.
        if (world.Get<ClaimsComponent>(context.Player).Account != 0)
        {
            world.Get<ServerSessionComponent>(context.Player)
                .Send(PersonSnapshotMapper.CreateCharacterInfoResponse(world, context.Player));
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonEnterGameServerRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        if (request.Session.Account != request.Account ||
            !await sessions.AnyAsync(e => e.Session == request.Session.Key))
        {
            session.Disconnect();
            return;
        }

        var mazeReserve = await mazeReserves
            .FindByIdAsync(request.Actor.Identifier.ToString());

        // Nobody asked to enter a maze with this person, or the reservation has expired.
        if (mazeReserve is null ||
            mazeReserve.Account != request.Account ||
            !batches.TryGetAndCache(mazeReserve.Location, out var batch, out var type) ||
            type != BatchType.Maze)
        {
            session.Disconnect();
            return;
        }

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var person = await database.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Account.Id == request.Account && e.Id == request.Actor.Identifier, context.CancellationToken);

        if (person is null)
        {
            session.Disconnect();
            return;
        }

        await mazeReserves
            .DeleteAsync(mazeReserve);

        world.Set(context.Player, new ClaimsComponent(request.Session));

        await leases
            .AttachAsync(context.Player, request.Session.Key, request.Session.Account);

        world.Set(context.Player, new WorldComponent
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

        world.Set(context.Player, new CurrencyComponent
        {
            Gold = 5_000_000,
            Cash = 7_000_000,
            BattlePoint = 10_000_000,
            Ether = 15_000_000
        });

        registry.PullPerson(context.Player, person);

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
            .DoFileAsync(Path.Join("scripts", $"{mazeInfo.Script}.lua"), context.CancellationToken);

        var creatureManager = new LuaCreatureManager(world, batch);

        creatureManager.Emplace(context.Player);

        var maze = new LuaMaze(state, world, context.Player, new LuaCreatureManager(world, batch), buffManager, questManager, batch, interactionCollection);

        world.Add(context.Player, maze);

        session.Send(PersonSnapshotMapper.CreateCharacterInfoResponse(world, context.Player));

        questManager.SendQuestEpisodeList(context.Player);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonChangeServerRequest request)
    {
        await leaving
            .LeaveAsync(context.Player, request.Destination);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonLoadTitleRequest request)
    {
        var list = titleCollection
            .Where(e => e is { Field2: 0, Field3: 0 })
            .Select(e => e.Id)
            .ToArray();

        var session = world.Get<ServerSessionComponent>(context.Player);

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
        var session = world.Get<ServerSessionComponent>(context.Player);

        // session.Send(new PersonTradePasswordResponse(request.Password));

        session.Send(new CharacterTradePasswordResponse
        {
            ProtectionState = PasswordProtectionState.Authenticated,
            ErrorCode = 0
        });

        return ValueTask.CompletedTask;
    }
}
