using Arch.Core;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Handler.Extensions;
using OpenWorker.Hotspot.Modules.Movement.Requests;

namespace OpenWorker.Benchmarks;

/// <summary>
/// Stands for the movement service: reads the request and keeps one number of it, so the reading is not optimised away.
/// </summary>
public sealed class MovementProbe : IHotspotHandler<MovementMoveRequest>
{
    public float Last { get; private set; }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MovementMoveRequest request)
    {
        Last = request.fPosX + request.fMoveSpeed;

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// One move request through <see cref="HandlerExecutor"/>: the opcode, the handler table, the request read from the
/// packet, the handler. The executor comes from the container, as in a server.
/// </summary>
[MemoryDiagnoser]
public class DispatchBenchmark
{
    private readonly byte[] _packet = Movement.Request();

    private World _world = null!;
    private ServiceProvider _services = null!;
    private HandlerExecutor _executor = null!;
    private Entity _player;

    /// <summary>
    /// Whether the executor takes a token from the bucket of the group of the packet, as a server does, or is built
    /// without a limiter, to see what the check costs.
    /// </summary>
    [Params(true, false)]
    public bool RateLimited { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = World.Create();

        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Hotspot:ErrorBudget:MaxErrors"] = "10",
                    ["Hotspot:ErrorBudget:Window"] = "00:01:00",

                    // The bucket of the moves is checked for every packet, and never runs dry here: what is measured is
                    // the way to the handler, not the drop of a packet over the limit.
                    ["Hotspot:RateLimits:Movement:PerSecond"] = "1000000000",
                    ["Hotspot:RateLimits:Movement:Burst"] = "1000000000"
                })
                .Build())
            .AddSingleton(_world)
            .AddHotspotHandlers()
            .BuildServiceProvider();

        _executor = RateLimited
            ? _services.GetRequiredService<HandlerExecutor>()
            : new HandlerExecutor(
                _services.GetRequiredService<HandlerTable>(),
                _services,
                _world,
                _services.GetRequiredService<HandlerErrorBudget>(),
                _services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HandlerExecutor>>());
        _player = _world.Create();
    }

    [Benchmark]
    public void DispatchAMove()
    {
        var dispatch = _executor.ExecuteAsync(_packet, _player, null, CancellationToken.None);

        if (!dispatch.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("The handler completes at once; the dispatch did not.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // A packet that failed would be counted against the session and measure the error path instead.
        var handled = _services.GetRequiredService<MovementProbe>().Last;

        _services.Dispose();
        _world.Dispose();

        if (handled != 501f)
        {
            throw new InvalidOperationException("The move did not reach its handler.");
        }
    }
}
