using System.Globalization;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One district or maze that names a <c>*.vbatch</c> map file.
/// </summary>
public sealed class BatchMapEntry
{
    public required short Id { get; init; }

    public required string BatchName { get; init; }

    public required string Path { get; init; }

    public required bool FileExists { get; init; }

    public required string Kind { get; init; }

    public string Title =>
        string.Format(CultureInfo.InvariantCulture, "{0} · {1}", Id, BatchName);

    public string Subtitle =>
        FileExists
            ? BatchName + ".vbatch"
            : "batch file missing";
}
