using System.Diagnostics.Metrics;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// The classes that measure take their metrics as an optional constructor parameter, so a container that does not pass them
/// would not fail: it would just not measure. These tests build the container of a server the way its <c>Program.cs</c> does and
/// check that what it builds is measured.
/// </summary>
[TestClass]
public sealed class TestMetricsWiring
{
    private static IHost CreateHost()
    {
        return new HostBuilder()
            .ConfigureAppConfiguration(configuration => configuration
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "Settings", "AuthServer.json"), optional: false))
            .ConfigureServices((context, services) =>
            {
                services.AddCache();
                services.AddPersistence();
                services.AddSingleton<SimulationClock>();
                services.AddHotspot(context, EntityComponentService.Auth);
            })
            .Build();
    }

    private static MetricCollector<T> Listen<T>(IHost host, string instrument) where T : struct
    {
        return new MetricCollector<T>(host.Services.GetRequiredService<IMeterFactory>(), OpenWorkerMeter.Name, instrument);
    }

    [TestMethod]
    public async Task TheWorldLoopOfTheContainerIsMeasured()
    {
        using var host = CreateHost();
        using var durations = Listen<double>(host, "worldloop.callback.duration");

        var loop = host.Services.GetRequiredService<WorldLoop>();

        await loop.InvokeAsync(() => { });

        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (durations.GetMeasurementSnapshot().Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.CancellationToken);
        }

        Assert.IsNotEmpty(durations.GetMeasurementSnapshot(), "The loop the container builds does not report to the meter.");
    }

    [TestMethod]
    public async Task TheHandlerExecutorOfTheContainerCountsItsErrors()
    {
        using var host = CreateHost();
        using var errors = Listen<long>(host, "handler.errors");

        var world = host.Services.GetRequiredService<World>();
        var executor = host.Services.GetRequiredService<HandlerExecutor>();

        // Shorter than an opcode.
        await executor.ExecuteAsync(new byte[1], world.Create(), TestContext.CancellationToken);

        Assert.AreEqual(1, errors.GetMeasurementSnapshot().Sum(e => e.Value), "The executor the container builds does not report to the meter.");
    }

    [TestMethod]
    public void TheSimulationClockOfTheContainerCountsItsSimulations()
    {
        using var host = CreateHost();
        using var count = Listen<int>(host, "simulation.count");

        var clock = host.Services.GetRequiredService<SimulationClock>();

        clock.Add(new Idle());

        count.RecordObservableInstruments();

        Assert.AreEqual(1, count.LastMeasurement!.Value, "The clock the container builds does not report to the meter.");
    }

    [TestMethod]
    public void EveryMetricClassIsRegistered()
    {
        using var host = CreateHost();

        // ServerService is internal and takes SessionMetrics without a default: the container must be able to give it.
        Assert.IsNotNull(host.Services.GetRequiredService<SessionMetrics>());
        Assert.IsNotNull(host.Services.GetRequiredService<RedisMetrics>());
        Assert.IsNotNull(host.Services.GetRequiredService<PostgresMetrics>());
    }

    private sealed class Idle : ISimulation
    {
        public void Advance(TimeSpan elapsed)
        {
        }
    }

    public TestContext TestContext { get; set; } = null!;
}
