using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Batch;
using OpenWorker.Server.Common;
using OpenWorker.Server.Common.Services;
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
using OpenWorker.Gameplay.Buffs;
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
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;
using Redis.OM.Searching;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Skill;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Maze;

namespace OpenWorker.MazeServer.Services;

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
    TradePasswordService passwords,
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
    AchievementManager achievements,
    SharePointManager sharePoints,
    PrologueService prologue,
    QuestCatalog questCatalog,
    MazeInstances instances,
    PartyMazeEvents partyEvents,
    PersonSkills skills,
    PlayerGauges gauges,
    HelperGear helperGear,
    MazeEntryLimits entryLimits,
    ILogger<PersonService> logger
) :
    IHotspotHandler<PersonEnterGameServerRequest>,
    IHotspotHandler<PersonInfoReqRequest>,
    IHotspotHandler<PersonChangeServerRequest>
{
    private short Gate { get; } = gate.Value.Id;

    private readonly RowIndex<short, MazeInfoRow> _mazes = mazeInfoCollection.IndexBy(e => e.Id);
    private readonly RowIndex<int, InteractionObjectRow> _interactions = interactionCollection.IndexBy(e => e.Id);

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
        if (!TryGetMazeInfo(_mazes, mazeReserve.Location, out var mazeInfo))
        {
            LogNoMazeInfo(logger, request.Account, request.Actor.Identifier, mazeReserve.Location);
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

        await inventories.LoadAsync(context.Player, person, context.CancellationToken);

        registry.PullPerson(context.Player, entry);

        // What the login did with the trade password on the server it came from holds here.
        await passwords.EnterAsync(context.Player, context.CancellationToken);

        // The skills are checked in the maze, and a character that starts in the prologue has seen no district to learn them.
        await skills.LoadAsync(context.Player, person.Id, person.Hero, context.CancellationToken);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        prologue.Enter(context.Player, person);

        // How often the person entered the mazes today: this one counts the entry as the person comes in.
        await entryLimits.EnterAsync(context.Player, person.Id, context.CancellationToken);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        // A party shares the instance its reservation names; a player alone gets one of their own.
        var instance = mazeReserve.Instance == Guid.Empty ? Guid.NewGuid() : mazeReserve.Instance;
        var maze = await EnterInstanceAsync(context.Player, instance, mazeReserve.Party, mazeInfo, batch, context.CancellationToken);

        if (!world.IsAlive(context.Player))
        {
            if (maze is not null)
            {
                await instances.AbandonAsync(maze, context.Player);
            }

            return;
        }

        if (maze is null)
        {
            session.Disconnect();
            return;
        }

        world.Add(context.Player, maze);

        maze.FollowQuests(questCatalog);

        maze.Creatures.Vitals.Initialize(context.Player);
        gauges.Fill(context.Player);
        results.Start(context.Player, mazeInfo.Id);

        // The entry is counted and the item the maze asks for is taken; a player whose item is gone from the bag is sent away.
        if (!entryLimits.Entered(context.Player, mazeInfo.Id))
        {
            session.Disconnect();
            return;
        }

        // A client that was already playing reads the answer of the play scene; one from the character selection, the record.
        session.Send(PersonSnapshotMapper.CreateEntryResponse(world, context.Player, request.FirstConnect));
        skills.Send(context.Player);
        creatureServices.Stats?.SendAll(context.Player);

        session.Send(PersonProfileServiceBase.CreateCommunityResponse(world.Get<CommunityComponent>(context.Player)));

        await questManager.EnterAsync(context.Player, person.Id, context.CancellationToken);

        await miscState.EnterMazeAsync(context.Player, person.Id, context.CancellationToken);
        await achievements.EnterAsync(context.Player, person.Id, context.CancellationToken);
        await sharePoints.EnterMazeAsync(context.Player, person.Id, context.CancellationToken);
        await community.EnterAsync(context.Player);

        // The client has the maze now: the count of the entries is told last.
        if (world.IsAlive(context.Player))
        {
            entryLimits.TellCount(context.Player, mazeInfo.Id);
        }
    }

    /// <summary>
    /// Joins the instance where it stands, or opens it when the player is the first to come. A maze whose instance is over
    /// by the time the player gets in, or that plays another maze, is opened anew.
    /// </summary>
    private async ValueTask<LuaMaze?> EnterInstanceAsync(Entity player, Guid instance, int party, MazeInfoRow mazeInfo, VBatchFile batch, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var maze = await instances.GetOrOpenAsync(instance, () => OpenAsync(player, instance, party, mazeInfo, batch, cancellationToken));

            // The player may have left while the maze was opened.
            if (!world.IsAlive(player))
            {
                return maze;
            }

            // The member who was opening it left before it was open: this one opens it.
            if (maze is null)
            {
                continue;
            }

            if (maze.Maze != mazeInfo.Id)
            {
                instance = Guid.NewGuid();
                continue;
            }

            if (maze.Members.Contains(player) || maze.Join(player))
            {
                return maze;
            }
        }

        return null;
    }

    private async ValueTask<LuaMaze?> OpenAsync(Entity player, Guid instance, int party, MazeInfoRow mazeInfo, VBatchFile batch, CancellationToken cancellationToken)
    {
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
            if (!await mazeScripts.RunAsync(state, mazeInfo.Script, cancellationToken))
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

        // Nobody to open the maze for any more: the player left while its script and mesh were read.
        if (!world.IsAlive(player))
        {
            return null;
        }

        var creatures = new LuaCreatureManager(world, batch, combatTables, creatureServices, creatureLogger, mesh);
        var maze = new LuaMaze(state, world, player, creatures, buffManager, questManager, batch, _interactions, skillActions, combatTables, mazeInfo, reports, creatureServices.Clock, mazeLogger)
        {
            Instance = instance,
            Party = party
        };

        // What happens in the instance is wired once, for every player who is in it then.
        maze.MonsterKilled += (killer, monster, position) => rewards.OnKilled(killer, PresentOf(maze), monster, position);
        maze.Completed += () => partyEvents.Completed(maze);
        maze.MonstersHit += results.OnMonstersHit;
        creatures.Vitals.HealthChanged += partyEvents.HealthChanged;
        creatures.Vitals.HealthChanged += results.OnHealthChanged;

        // A helper sent away, by whatever way, gives back the orb of a friend it wore.
        creatures.Helpers.Released += helper => helperGear.ReleaseOrb(helper.Owner, helper.Helper);

        return maze;
    }

    /// <summary>
    /// The players of the maze who stand to share what a kill gives: those still on their feet.
    /// </summary>
    private static Entity[] PresentOf(LuaMaze maze)
    {
        return maze.Members.Where(maze.Creatures.Vitals.IsAlive).ToArray();
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonChangeServerRequest request)
    {
        await leaving
            .LeaveAsync(context.Player, request.Destination);
    }

    /// <summary>
    /// The row of the maze table for the location, or nothing when the table has none.
    /// </summary>
    public static bool TryGetMazeInfo(RowIndex<short, MazeInfoRow> table, short location, out MazeInfoRow info)
    {
        return table.TryGet(location, out info);
    }

    private void SendSoulMetryList(ServerSessionComponent session)
    {
        var list = soulMetryCollection
            .Select(e => new SoulMetryValue(e.Id, 0))
            .ToArray();

        session.Send(new SoulMetryListResponse { Values = list });
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
