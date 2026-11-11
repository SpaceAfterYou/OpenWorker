using System.Globalization;
using OpenWorker.Content.Tool.Models;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Lists the Havok navigation meshes of the client folder.
/// </summary>
internal static class NavMeshCatalog
{
    public static IReadOnlyList<NavMeshEntry> LoadEntries(string? folder, out string status)
    {
        if (folder is null)
        {
            status = string.Format(
                CultureInfo.InvariantCulture,
                "Set {0} to the folder with *.hkt navigation meshes.",
                ContentPaths.NavMeshVariable);
            return [];
        }

        var entries = Directory.EnumerateFiles(folder, "*.hkt")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new NavMeshEntry
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                FileExists = true
            })
            .ToArray();

        status = string.Format(
            CultureInfo.InvariantCulture,
            "Navigation: {0} meshes in {1}",
            entries.Length,
            folder);
        return entries;
    }
}
