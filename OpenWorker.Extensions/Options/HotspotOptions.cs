using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// How the connection layer behaves, in the <c>Hotspot</c> section.
/// </summary>
public sealed class HotspotOptions : IValidatableObject
{
    public const string SectionName = "Hotspot";

    /// <summary>
    /// Where Debug builds save every received message, one file per opcode. Empty turns the dumps off.
    /// </summary>
    public string PacketDumpPath { get; set; } = string.Empty;

    /// <summary>
    /// How long a connection may stay open before it enters: before it logs in on the auth server, or names the session it
    /// came with on the gate, a district or a maze. A connection that has not entered by then is closed. Written as a
    /// time span: <c>00:01:00</c> is one minute, the default. The client sends its entry the moment it connects to the
    /// gate, a district or a maze, so a minute covers the wait for the server it leaves and the read of the person many
    /// times over. It connects to the auth server when the program starts, before its login window opens, and a person
    /// types the password into that window: the auth server gives it longer.
    /// </summary>
    public TimeSpan EntryTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EntryTimeout <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Hotspot:EntryTimeout must be a time longer than zero, for example 00:01:00.", [nameof(EntryTimeout)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class HotspotOptionsValidator : IValidateOptions<HotspotOptions>;
