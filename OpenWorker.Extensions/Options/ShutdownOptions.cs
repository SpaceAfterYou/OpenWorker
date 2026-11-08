using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// How long a server may take to stop, in the <c>Shutdown</c> section. A stopping server first drains: it stops
/// accepting connections, takes its records back from Redis and closes every session (the close handlers save the
/// players, the writers send what is queued), and only then do the other services stop and the world loop end.
/// </summary>
public sealed class ShutdownOptions : IValidatableObject
{
    public const string SectionName = "Shutdown";

    /// <summary>
    /// How long the host waits for the whole stop, drain included. Written as a time span: <c>00:00:30</c> is thirty
    /// seconds. A container is killed when its stop grace period ends, so <c>compose.yaml</c> gives it more time than this.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// How long the drain may take. Sessions that have not closed by then are left, and the server goes on stopping.
    /// It has to be shorter than <see cref="Timeout"/>, so that the services stopped after the drain still get their time.
    /// </summary>
    public TimeSpan DrainTimeout { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DrainTimeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Shutdown:DrainTimeout must be a time longer than zero, for example 00:00:15.", [nameof(DrainTimeout)]);
        }

        if (Timeout <= DrainTimeout)
        {
            yield return new ValidationResult("Shutdown:Timeout must be longer than Shutdown:DrainTimeout, for example 00:00:30.", [nameof(Timeout)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class ShutdownOptionsValidator : IValidateOptions<ShutdownOptions>;
