using System.Globalization;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One client <c>*.hkt</c> navigation mesh file.
/// </summary>
public sealed class NavMeshEntry
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    public required bool FileExists { get; init; }

    public string Title => Name;

    public string Subtitle =>
        FileExists
            ? Name + ".hkt"
            : "navmesh file missing";

    public string FormatCounts(int faces, int edges, int vertices) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}: {1} faces, {2} edges, {3} vertices",
            Name,
            faces,
            edges,
            vertices);
}
