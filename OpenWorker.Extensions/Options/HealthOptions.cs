using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where the health endpoint listens and how long its checks may take, in the <c>Health</c> section. The endpoint
/// answers <c>/healthz</c> (the process is alive and its world loop runs) and <c>/readyz</c> (the server can take
/// players) on a port of its own, apart from the one players connect to.
/// </summary>
public sealed class HealthOptions : IValidatableObject
{
    public const string SectionName = "Health";

    /// <summary>
    /// The port inside the container; <c>compose.yaml</c> publishes it where it is useful.
    /// </summary>
    [NumberRange(1, PortRange.ListenMax, ErrorMessage = "Health:Port must be a TCP port from 1 to 65535.")]
    public int Port { get; set; }

    /// <summary>
    /// How long the world loop may take to run an empty callback before the server counts as stuck. Written as a
    /// time span: <c>00:00:05</c> is five seconds.
    /// </summary>
    public TimeSpan LoopTimeout { get; set; }

    /// <summary>
    /// How long Redis and the database may take to answer a check before they count as down.
    /// </summary>
    public TimeSpan CheckTimeout { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LoopTimeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Health:LoopTimeout must be a time longer than zero, for example 00:00:05.", [nameof(LoopTimeout)]);
        }

        if (CheckTimeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Health:CheckTimeout must be a time longer than zero, for example 00:00:03.", [nameof(CheckTimeout)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class HealthOptionsValidator : IValidateOptions<HealthOptions>;

/// <summary>
/// The health endpoint cannot share the port players connect to: one of the two would not start, and the message of
/// the failure would not name the setting.
/// </summary>
internal sealed class HealthPortValidator(IConfiguration configuration) : IValidateOptions<HealthOptions>
{
    public ValidateOptionsResult Validate(string? name, HealthOptions options)
    {
        var server = configuration.GetSection(ServerOptions.SectionName).GetValue<int?>(nameof(ServerOptions.Port));

        return server is { } port && port == options.Port
            ? ValidateOptionsResult.Fail("Health:Port must differ from Server:Port: players connect to the one, the probes to the other.")
            : ValidateOptionsResult.Success;
    }
}
