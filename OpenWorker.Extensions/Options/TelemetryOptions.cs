using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// How the metrics are carried to the collector.
/// </summary>
public enum TelemetryProtocol
{
    /// <summary>
    /// OTLP over HTTP with protobuf bodies. Prometheus takes this one.
    /// </summary>
    HttpProtobuf,

    /// <summary>
    /// OTLP over gRPC.
    /// </summary>
    Grpc
}

/// <summary>
/// Where the metrics of the server are sent, in the <c>Telemetry</c> section. The metrics are recorded either way; the
/// endpoint only says whether they leave the process.
/// </summary>
public sealed class TelemetryOptions : IValidatableObject
{
    public const string SectionName = "Telemetry";

    /// <summary>
    /// The OTLP endpoint of the collector. Empty turns the export off. With <see cref="TelemetryProtocol.HttpProtobuf"/> it is
    /// the whole address of the metrics endpoint, <c>http://prometheus:9090/api/v1/otlp/v1/metrics</c> for Prometheus.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    public TelemetryProtocol Protocol { get; set; }

    /// <summary>
    /// How often the metrics are sent. Written as a time span: <c>00:00:10</c> is ten seconds.
    /// </summary>
    public TimeSpan ExportInterval { get; set; }

    /// <summary>
    /// The metrics are sent to a collector.
    /// </summary>
    public bool IsExporting => !string.IsNullOrWhiteSpace(Endpoint);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsExporting &&
            !(Uri.TryCreate(Endpoint, UriKind.Absolute, out var address) && address.Scheme is "http" or "https" && address.Host.Length > 0))
        {
            yield return new ValidationResult(
                "Telemetry:Endpoint must be empty (no export) or an address like http://prometheus:9090/api/v1/otlp/v1/metrics.", [nameof(Endpoint)]);
        }

        if (!Enum.IsDefined(Protocol))
        {
            yield return new ValidationResult("Telemetry:Protocol must be HttpProtobuf or Grpc.", [nameof(Protocol)]);
        }

        if (ExportInterval < TimeSpan.FromSeconds(1))
        {
            yield return new ValidationResult(
                "Telemetry:ExportInterval must be a time of a second or more, for example 00:00:10.", [nameof(ExportInterval)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class TelemetryOptionsValidator : IValidateOptions<TelemetryOptions>;
