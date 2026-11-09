using System.Numerics;
using Arch.Core;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Channel;
using OpenWorker.DependencyInjection;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Wire;

namespace OpenWorker.Benchmarks;

/// <summary>
/// One move of a player in a channel of <see cref="Players"/>, <see cref="InSight"/> of whom (the mover among them) stand
/// in the cells around it and the others far away: the movement handler of the district checks the speed, finds the cell
/// unchanged, encodes the move once and queues it to the players in sight, whose writers send it to a stream that drops
/// it. Off the world loop a move goes at once, so this is the cost of a pass that holds one move. It should not follow
/// <see cref="Players"/>. A broadcast to the whole channel, what every move was before, is not measured beside it: queued to
/// 500 writers per operation it outruns them, and they close their sessions (<see cref="BroadcastBenchmark"/> has 50).
/// </summary>
[MemoryDiagnoser]
public class NearbyMoveBenchmark
{
    /// <summary>
    /// The players who see the mover, the mover included: a fixed density.
    /// </summary>
    public const int InSight = 16;

    private const float Cell = WorldOptions.DefaultVisibilityRadius;

    private readonly List<ServerSessionComponent> _sessions = [];

    private World _world = null!;
    private ServiceChannels _channels = null!;
    private MovementService _service = null!;
    private ServiceHandleContext _context;
    private MovementMoveRequest[] _requests = [];
    private int _step;

    [Params(64, 500)]
    public int Players { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // The provider looks for the types of the components in the loaded assemblies: load them first. A discarded typeof
        // may be optimised away in the process of a benchmark, the assembly of a type may not.
        GC.KeepAlive(typeof(ChannelMemberComponent).Assembly);
        GC.KeepAlive(typeof(ServiceChannels).Assembly);
        GC.KeepAlive(typeof(MovementService).Assembly);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigurationUtils.InstanceTargetKey] = nameof(EntityComponentService.District) })
            .Build();

        var provider = new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance);

        _world = World.Create();
        _channels = new ServiceChannels(Options.Create(new ChannelOptions { From = 1, To = 1 }), _world);

        Entity mover = default;

        for (var index = 0; index < Players; index++)
        {
            // The ones in sight share the cell of the mover; the others stand in a square of cells three and more away.
            var place = index < InSight
                ? new Vector3(100f + index, 100f, 0f)
                : new Vector3(Cell * (3 + ((index - InSight) % 25)), Cell * (3 + ((index - InSight) / 25)), 0f);

            var player = Create(provider, index, place);

            _channels.Join(0, player);

            if (index == 0)
            {
                mover = player;
            }
        }

        var table = new HandlerTable(new HandlerTableBuilder(new HandlerBuilder(), NullLogger<HandlerTable>.Instance));
        var budget = new HandlerErrorBudget(TimeProvider.System, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.FromMinutes(1) }));
        var errors = new HandlerExecutor(table, new ServiceCollection().BuildServiceProvider(), _world, budget, NullLogger<HandlerExecutor>.Instance);
        var pace = new MovementPace(new Dictionary<Hero, float> { [Hero.Lily] = 560f }, new WorldOptions(), TimeProvider.System);

        _service = new MovementService(_world, _channels, pace, errors, NullLogger<MovementService>.Instance);
        _context = new ServiceHandleContext(mover, CancellationToken.None);

        // Back and forth by a hair, far slower than a run, so the check of the speed passes and the cell stays.
        _requests = [Request(_world.Get<ActorComponent>(mover).Identifier, 100f), Request(_world.Get<ActorComponent>(mover).Identifier, 100.001f)];
    }

    /// <summary>
    /// The move through the handler to the players in sight.
    /// </summary>
    [Benchmark]
    public void MoveToThoseInSight()
    {
        _step ^= 1;

        var handled = _service.OnHandleAsync(_context, _requests[_step]);

        if (!handled.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("The handler completes at once; the move did not.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // A writer that fell behind closes its session, and the numbers would be of nothing.
        var closed = _sessions.Count(e => e.Closed.IsCompleted);

        foreach (var session in _sessions)
        {
            session.Disconnect();
        }

        _world.Dispose();

        if (closed > 0)
        {
            throw new InvalidOperationException($"{closed} sessions closed during the run.");
        }
    }

    private Entity Create(ArchComponentProvider provider, int index, Vector3 place)
    {
        var player = provider.CreateEntity(_world);
        var actor = 1000 + index;
        var session = new ServerSessionComponent(Stream.Null);

        _sessions.Add(session);

        Put(player, session);
        Put(player, new ActorComponent(actor, ActorType.User));
        Put(player, new ClaimsComponent(account: actor, salt: 1));
        Put(player, new PersonInfoComponent { Name = "Player" + index, Hero = Hero.Lily });
        Put(player, new LeagueComponent { Id = 0, Name = string.Empty, Card = new LeagueComponentCard { Border = 0, Emblem = 0 } });
        Put(player, new ShopPrivateComponent { Type = 0, Name = string.Empty });
        Put(player, new WorldComponent { Location = 10003, Position = place, Rotation = 0f, Jump = 0 });

        return player;
    }

    /// <summary>
    /// Sets a component, or adds it when the provider did not give it: the provider gives the components of the assemblies
    /// loaded when it looks, and a benchmark process may not have loaded them all yet.
    /// </summary>
    private void Put<T>(Entity player, in T component)
    {
        if (_world.Has<T>(player))
        {
            _world.Set(player, component);
        }

        else
        {
            _world.Add(player, component);
        }
    }

    private static MovementMoveRequest Request(int actor, float x)
    {
        Span<byte> bytes = stackalloc byte[4 + 8 + (6 * 4) + 1 + 4 + 4 + 1];

        var writer = new PacketWriter(bytes);

        writer.WriteInt32(actor);
        writer.WriteInt64(0x0001_2713_0000_0000);
        writer.WriteSingle(x);
        writer.WriteSingle(100f);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteSingle(x);
        writer.WriteSingle(100f);
        writer.WriteByte(1);
        writer.WriteSingle(0f);
        writer.WriteSingle(560f);
        writer.WriteByte(0);

        var reader = new PacketReader(bytes);

        return MovementMoveRequest.Read(ref reader);
    }
}
