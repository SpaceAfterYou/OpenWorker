using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Batch.Actions;
using OpenWorker.Hotspot.Cache;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using Arch.Core;
using Lua;
using Lua.Standard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.State;
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
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Persistence;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed partial class PersonService(
    World world,
    ReadOnlyCollection<SoulMetryRow> soulMetryCollection,
    ReadOnlyCollection<MazeInfoRow> mazeInfoCollection,
    ReadOnlyCollection<InteractionObjectRow> interactionCollection,
    IRedisCollection<SessionCache> sessions,
    SessionLeaseManager leases,
    BatchManager batches,
    IRedisCollection<MazeReserveCache> mazeReserves,
    IDbContextFactory<PersistenceContext> factory,
    PersonRegistry registry,
    InventoryPersistence inventories,
    KillRewards rewards,
    MazeResults results,
    LeaveManager leaving,
    BuffManager buffManager,
    QuestManager questManager,
    SkillActionProvider skillActions,
    CombatTables combatTables,
    MiscStateLoader miscState,
    MazeReporter reports,
    CreatureServices creatureServices,
    MazeScripts mazeScripts,
    ILogger<LuaCreatureManager> creatureLogger,
    ILogger<LuaMaze> mazeLogger,
    ILogger<LuaGameHelper> helperLogger,
    IOptions<GateOptions> gate,
    CommunityNetwork community,
    ILogger<PersonService> logger
) :
    IHotspotHandler<PersonEnterGameServerRequest>,
    IHotspotHandler<PersonInfoReqRequest>,
    IHotspotHandler<PersonTradePasswordRequest>,
    IHotspotHandler<PersonChangeServerRequest>
{
    private short Gate { get; } = gate.Value.Id;

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
            .FindByIdAsync(request.Actor.Identifier.ToString(CultureInfo.InvariantCulture));

        // Nobody asked to enter a maze with this person, or the reservation has expired.
        if (mazeReserve is null)
        {
            LogNoReservation(logger, request.Account, request.Actor.Identifier);
            session.Disconnect();
            return;
        }

        // The reservation of another account stays: its owner still has to come.
        if (mazeReserve.Account != request.Account)
        {
            LogForeignReservation(logger, request.Account, request.Actor.Identifier, mazeReserve.Account);
            session.Disconnect();
            return;
        }

        if (!batches.TryGetAndCache(mazeReserve.Location, out var batch, out var type) || type != BatchType.Maze)
        {
            LogNoMazeBatch(logger, request.Account, request.Actor.Identifier, mazeReserve.Location);
            session.Disconnect();
            return;
        }

        // Everything the entry needs is looked up before the reservation is used up: a client that is turned away
        // for a row the tables do not have must not wait on the loop, and its reservation is not spent on it.
        if (!TryGetMazeInfo(mazeInfoCollection, mazeReserve.Location, out var mazeInfo))
        {
            LogNoMazeInfo(logger, request.Account, request.Actor.Identifier, mazeReserve.Location);
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

        world.SetLogAccount(context.Player, request.Session.Account);

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
            Cash = 7_000_000
        });

        await inventories.LoadAsync(context.Player, person, context.CancellationToken);

        registry.PullPerson(context.Player, person);

        var state = LuaState.Create();
        state.OpenStandardLibraries();

        LuaCompatibility.Apply(state);

        var table = new LuaTable
        {
            ["GetHelper"] = new LuaFunction((ctx, _) =>
            {
                ctx.Return(new LuaGameHelper(helperLogger));
                return ValueTask.FromResult(1);
            })
        };

        state.Environment["Soulworker"] = new LuaValue(table);


        // A few mazes have no script among the files of the client, and are played without one. The script is read and
        // compiled once, off the loop; each maze runs the compiled chunk in its own state.
        try
        {
            if (!await mazeScripts.RunAsync(state, mazeInfo.Script, context.CancellationToken))
            {
                LogNoScript(logger, mazeScripts.PathOf(mazeInfo.Script));
            }
        }

        // A script that does not load is no fault of the client: the maze runs without it.
        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogScriptFailed(logger, e, mazeScripts.PathOf(mazeInfo.Script));
        }

        var (mesh, meshError) = await creatureServices.Meshes.GetAsync(mazeInfo.Batch);

        if (mesh is null)
        {
            LogNoNavigationMesh(logger, mazeInfo.Batch, meshError);
        }

        var creatures = new LuaCreatureManager(world, batch, combatTables, creatureServices, creatureLogger, mesh);
        var maze = new LuaMaze(state, world, context.Player, creatures, buffManager, questManager, batch, interactionCollection, skillActions, combatTables, mazeInfo, reports, creatureServices.Clock, mazeLogger);

        world.Add(context.Player, maze);

        creatures.Attach(context.Player);
        creatures.Vitals.Initialize(context.Player);
        maze.MonsterKilled += (monster, position) => rewards.OnKilled(context.Player, monster, position);
        maze.Completed += () => results.Complete(context.Player);
        results.Start(context.Player);

        session.Send(PersonSnapshotMapper.CreateCharacterInfoResponse(world, context.Player));

        session.Send(PersonProfileServiceBase.CreateCommunityResponse(world.Get<CommunityComponent>(context.Player)));

        await questManager.EnterAsync(context.Player, person.Id, context.CancellationToken);

        await miscState.EnterMazeAsync(context.Player, person.Id, context.CancellationToken);
        await community.EnterAsync(context.Player);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonChangeServerRequest request)
    {
        await leaving
            .LeaveAsync(context.Player, request.Destination);
    }

    /// <summary>
    /// The row of the maze table for the location, or nothing when the table has none.
    /// </summary>
    public static bool TryGetMazeInfo(ReadOnlyCollection<MazeInfoRow> table, short location, out MazeInfoRow info)
    {
        foreach (var row in table)
        {
            if (row.Id == location)
            {
                info = row;

                return true;
            }
        }

        info = default;

        return false;
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} came to a maze as person {Person} with no reservation, or with one that has expired, and is turned away")]
    private static partial void LogNoReservation(ILogger logger, int account, int person);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} came to a maze as person {Person}, but the reservation of that person belongs to account {ReservedAccount}, and it is turned away")]
    private static partial void LogForeignReservation(ILogger logger, int account, int person, int reservedAccount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} came to a maze as person {Person}, but the location {Location} has no maze batch, and it is turned away")]
    private static partial void LogNoMazeBatch(ILogger logger, int account, int person, short location);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} came to a maze as person {Person}, but the maze table has no row for the location {Location}, and it is turned away")]
    private static partial void LogNoMazeInfo(ILogger logger, int account, int person, short location);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The maze {Maze} has no navigation mesh, its monsters walk where they like: {Error}")]
    private static partial void LogNoNavigationMesh(ILogger logger, string maze, string? error);

    [LoggerMessage(Level = LogLevel.Error, Message = "The script {Script} of the maze failed to load, the maze runs without it")]
    private static partial void LogScriptFailed(ILogger logger, Exception exception, string script);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The script {Script} of the maze is missing, the maze runs without it")]
    private static partial void LogNoScript(ILogger logger, string script);
}
