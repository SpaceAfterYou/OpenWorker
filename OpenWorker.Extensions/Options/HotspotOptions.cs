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

    /// <summary>
    /// The longest <see cref="EntryTimeout"/>: a day is far beyond any wait for a login, and well inside what the timer of a
    /// connection can count.
    /// </summary>
    public static TimeSpan MaxEntryTimeout { get; } = TimeSpan.FromDays(1);

    /// <summary>
    /// How long a connection may stay silent before the system asks the other end whether it is still there (the keep-alive
    /// of TCP, then a probe every ten seconds, five of them). A client that is gone without closing its connection, a cable
    /// pulled or a machine stopped, has its connection closed then. The client answers the keep-alive message of the game
    /// only in a district and a maze; at the login, the server list and the character selection only this finds that it is
    /// gone. Zero turns it off.
    /// </summary>
    public TimeSpan ConnectionKeepAlive { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The most connections one address may hold on this server at once; one more is closed as soon as it is accepted.
    /// Zero, the default, sets no limit. Players behind one address (a household, a café, a carrier) share it, and so does
    /// every client behind a proxy that does not pass on the client's address, the test stand's included.
    /// </summary>
    public int MaxConnectionsPerAddress { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EntryTimeout <= TimeSpan.Zero || EntryTimeout > MaxEntryTimeout)
        {
            yield return new ValidationResult(
                "Hotspot:EntryTimeout must be a time longer than zero and at most a day (1.00:00:00), for example 00:01:00.", [nameof(EntryTimeout)]);
        }

        if (ConnectionKeepAlive < TimeSpan.Zero || ConnectionKeepAlive > MaxEntryTimeout)
        {
            yield return new ValidationResult(
                "Hotspot:ConnectionKeepAlive must be zero (off) or a time of at most a day (1.00:00:00), for example 00:01:00.", [nameof(ConnectionKeepAlive)]);
        }

        if (MaxConnectionsPerAddress < 0)
        {
            yield return new ValidationResult(
                "Hotspot:MaxConnectionsPerAddress must be zero (no limit) or more.", [nameof(MaxConnectionsPerAddress)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class HotspotOptionsValidator : IValidateOptions<HotspotOptions>;
