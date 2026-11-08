using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenWorker.DependencyInjection;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// Where the metrics go: the export that can be switched off, and how a process names itself.
/// The instruments themselves are tested next to what they measure, in <c>OpenWorker.Hotspot.Test</c>.
/// </summary>
[TestClass]
public sealed class TestTelemetry
{
    private const string Interval = "00:00:10";

    private static IHost CreateHost(Dictionary<string, string?> settings, bool withInstance = false)
    {
        return new HostBuilder()
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings))
            .ConfigureServices((_, services) =>
            {
                if (withInstance)
                {
                    services.AddInstanceOptions();
                    services.AddGateOptions();
                }

                services.AddTelemetry();
            })
            .Build();
    }

    /// <summary>
    /// Whether anything listens to the meter of the server: a counter of that meter is only enabled when something does.
    /// </summary>
    private static bool IsListenedTo(IHost host)
    {
        var meter = host.Services.GetRequiredService<IMeterFactory>().Create(OpenWorkerMeter.Name);

        return meter.CreateCounter<long>("probe").Enabled;
    }

    [TestMethod]
    public async Task WithoutAnEndpointNothingListensAndRecordingIsFree()
    {
        using var host = CreateHost(new()
        {
            ["Telemetry:Endpoint"] = "",
            ["Telemetry:Protocol"] = "HttpProtobuf",
            ["Telemetry:ExportInterval"] = Interval
        });

        await host.StartAsync(TestContext.CancellationToken);

        Assert.IsFalse(IsListenedTo(host), "With the export off no reader should switch the instruments on.");

        await host.StopAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task WithAnEndpointTheMetersAreExported()
    {
        // Nothing listens at this address: the export fails quietly and the server goes on.
        using var host = CreateHost(new()
        {
            ["Telemetry:Endpoint"] = "http://127.0.0.1:1/api/v1/otlp/v1/metrics",
            ["Telemetry:Protocol"] = "HttpProtobuf",
            ["Telemetry:ExportInterval"] = Interval
        });

        await host.StartAsync(TestContext.CancellationToken);

        Assert.IsTrue(IsListenedTo(host));

        await host.StopAsync(TestContext.CancellationToken);
    }

    private static Dictionary<string, object> Describe(IHost host)
    {
        return host.Services.GetRequiredService<MeterProvider>().GetResource().Attributes.ToDictionary(e => e.Key, e => e.Value);
    }

    [TestMethod]
    public async Task ADistrictIsToldApartByItsIdentifierItsLocationAndItsGate()
    {
        var identifier = Guid.NewGuid();

        using var host = CreateHost(
            new()
            {
                ["Telemetry:Endpoint"] = "",
                ["Telemetry:Protocol"] = "HttpProtobuf",
                ["Telemetry:ExportInterval"] = Interval,
                [ConfigurationUtils.InstanceIdentifierKey] = identifier.ToString(),
                ["Instance:Location"] = "10003",
                ["Instance:PublicHost"] = "127.0.0.1",
                ["Gate:Id"] = "1"
            },
            withInstance: true);

        await host.StartAsync(TestContext.CancellationToken);

        var resource = Describe(host);

        Assert.AreEqual(identifier.ToString(), resource["service.instance.id"]);
        Assert.AreEqual("10003", resource["openworker.location"]);
        Assert.AreEqual("1", resource["openworker.gate"]);
        Assert.IsFalse(string.IsNullOrWhiteSpace((string)resource["service.name"]));

        await host.StopAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task AProcessWithNoIdentifierGetsOneOfItsOwnAndNoLocation()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Telemetry:Endpoint"] = "",
            ["Telemetry:Protocol"] = "HttpProtobuf",
            ["Telemetry:ExportInterval"] = Interval
        };

        using var first = CreateHost(settings);
        using var second = CreateHost(settings);

        await first.StartAsync(TestContext.CancellationToken);
        await second.StartAsync(TestContext.CancellationToken);

        var one = Describe(first);
        var other = Describe(second);

        Assert.IsTrue(Guid.TryParse((string)one["service.instance.id"], out var identifier) && identifier != Guid.Empty);
        Assert.AreNotEqual(one["service.instance.id"], other["service.instance.id"]);
        Assert.IsFalse(one.ContainsKey("openworker.location"));
        Assert.IsFalse(one.ContainsKey("openworker.gate"));

        await first.StopAsync(TestContext.CancellationToken);
        await second.StopAsync(TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = null!;
}
