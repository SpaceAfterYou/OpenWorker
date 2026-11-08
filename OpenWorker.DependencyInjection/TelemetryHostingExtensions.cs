using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.DependencyInjection;

public static class TelemetryHostingExtensions
{
    /// <summary>
    /// The runtime meter of .NET 9 and later: <c>dotnet.gc.*</c>, <c>dotnet.thread_pool.*</c>, <c>dotnet.process.*</c>, <c>dotnet.jit.*</c>.
    /// It ships in the runtime, so the instrumentation package for it is not needed.
    /// </summary>
    private const string RuntimeMeterName = "System.Runtime";

    /// <summary>
    /// Records the metrics of the process (the <see cref="OpenWorkerMeter.Name"/> meter and the runtime) and sends them over OTLP to the
    /// collector of <c>Telemetry:Endpoint</c>; with no endpoint nothing is sent and the instruments stay switched off, so
    /// recording costs next to nothing. The process is named after its executable (<c>service.name</c>) and told apart from the
    /// others by an identifier, the location of a district and the gate.<br/>
    /// A server that runs a world loop gets this with <see cref="HotspotHostingExtensions.AddHotspot"/>.
    /// </summary>
    public static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        services.AddTelemetryOptions();
        services.AddOpenWorkerMetrics();

        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddMeter(OpenWorkerMeter.Name)
                .AddMeter(RuntimeMeterName));

        services.ConfigureOpenTelemetryMeterProvider(Configure);

        return services;
    }

    /// <summary>
    /// The settings are read here, when the host builds the provider, so that they are the validated options and not a copy
    /// taken while the services were still being registered.
    /// </summary>
    private static void Configure(IServiceProvider provider, MeterProviderBuilder metrics)
    {
        var options = provider.GetRequiredService<IOptions<TelemetryOptions>>().Value;

        metrics.ConfigureResource(resource => Describe(resource, provider));

        if (!options.IsExporting)
        {
            return;
        }

        // AddOtlpExporter registers services, which cannot be done once the container is built; the reader is made by hand.
        var exporter = new OtlpExporterOptions
        {
            Endpoint = new Uri(options.Endpoint),
            Protocol = options.Protocol == TelemetryProtocol.Grpc ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf
        };

        // Prometheus keeps the totals, and works out the rates itself. The provider disposes the reader, and the reader the exporter.
        metrics.AddReader(new PeriodicExportingMetricReader(new OtlpMetricExporter(exporter), (int)options.ExportInterval.TotalMilliseconds)
        {
            TemporalityPreference = MetricReaderTemporalityPreference.Cumulative
        });
    }

    private static void Describe(ResourceBuilder resource, IServiceProvider provider)
    {
        var instance = provider.GetRequiredService<IOptions<InstanceOptions>>().Value;
        var gate = provider.GetRequiredService<IOptions<GateOptions>>().Value;

        // The district and the maze servers assign themselves an identifier; the others get one for the life of the process.
        var identifier = instance.Identifier == Guid.Empty ? Guid.CreateVersion7() : instance.Identifier;

        var attributes = new List<KeyValuePair<string, object>>();

        if (instance.Location > 0)
        {
            attributes.Add(new("openworker.location", instance.Location.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (gate.Id > 0)
        {
            attributes.Add(new("openworker.gate", gate.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        resource
            .AddService(
                serviceName: Assembly.GetEntryAssembly()?.GetName().Name ?? OpenWorkerMeter.Name,
                serviceInstanceId: identifier.ToString("D"),
                autoGenerateServiceInstanceId: false)
            .AddAttributes(attributes);
    }
}
