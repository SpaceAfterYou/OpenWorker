namespace OpenWorker.Extensions;

/// <summary>
/// How the connection layer behaves, in the <c>Hotspot</c> section.
/// </summary>
public sealed class HotspotOptions
{
    public const string SectionName = "Hotspot";

    /// <summary>
    /// Where Debug builds save every received message, one file per opcode. Empty turns the dumps off.
    /// </summary>
    public string PacketDumpPath { get; set; } = string.Empty;
}
