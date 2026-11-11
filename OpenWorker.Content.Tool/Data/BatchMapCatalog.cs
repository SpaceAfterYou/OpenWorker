using System.Globalization;
using System.Numerics;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Content.Tool.Models;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Lists districts and mazes that name a batch file, and builds the ground rectangles of a map.
/// </summary>
internal static class BatchMapCatalog
{
    public static IReadOnlyList<BatchMapEntry> LoadDistricts(string resFolder, string? batchFolder, out string status)
    {
        var rows = ResTableCache.Shared.Districts(resFolder);
        var entries = rows
            .Where(row => row.Id != 0 && !string.IsNullOrWhiteSpace(row.Batch))
            .OrderBy(row => row.Id)
            .Select(row => ToEntry(row.Id, row.Batch, batchFolder, "District"))
            .ToArray();

        status = FormatStatus("Districts", entries);
        return entries;
    }

    public static IReadOnlyList<BatchMapEntry> LoadMazes(string resFolder, string? batchFolder, out string status)
    {
        var rows = ResTableCache.Shared.Mazes(resFolder);
        var entries = rows
            .Where(row => row.Id != 0 && !string.IsNullOrWhiteSpace(row.Batch))
            .OrderBy(row => row.Id)
            .Select(row => ToEntry(row.Id, row.Batch, batchFolder, "Maze"))
            .ToArray();

        status = FormatStatus("Mazes", entries);
        return entries;
    }

    public static IReadOnlyList<BatchBoxDraw> LoadBoxes(string path)
    {
        var file = BatchFileCache.Shared.Load(path);
        return file.EventBox.AllBoxes
            .Select(ToDraw)
            .ToArray();
    }

    private static BatchMapEntry ToEntry(short id, string batch, string? batchFolder, string kind)
    {
        var name = batch.Trim();
        if (name.EndsWith(".vbatch", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^".vbatch".Length];
        }

        var path = batchFolder is null
            ? string.Empty
            : Path.Combine(batchFolder, name + ".vbatch");

        return new BatchMapEntry
        {
            Id = id,
            BatchName = name,
            Path = path,
            FileExists = path.Length > 0 && File.Exists(path),
            Kind = kind
        };
    }

    private static BatchBoxDraw ToDraw(BasicEntity box)
    {
        return new BatchBoxDraw
        {
            Id = box.Id,
            Kind = box.GetType().Name,
            TopLeft = box.PosTopLeft,
            BottomRight = box.PosBottomRight,
            RotationDegrees = box.Rotation,
            Color = ColorOf(box.GetType().Name)
        };
    }

    private static Vector3 ColorOf(string kind)
    {
        var hash = 2166136261u;
        foreach (var c in kind)
        {
            hash = (hash ^ c) * 16777619u;
        }

        var r = 0.35f + (hash & 0xFF) / 255f * 0.55f;
        var g = 0.35f + ((hash >> 8) & 0xFF) / 255f * 0.55f;
        var b = 0.35f + ((hash >> 16) & 0xFF) / 255f * 0.55f;
        return new Vector3(r, g, b);
    }

    private static string FormatStatus(string label, BatchMapEntry[] entries)
    {
        var missing = 0;
        foreach (var entry in entries)
        {
            if (!entry.FileExists)
            {
                missing++;
            }
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}: {1} maps{2}",
            label,
            entries.Length,
            missing == 0
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, ", {0} batch files missing", missing));
    }
}
