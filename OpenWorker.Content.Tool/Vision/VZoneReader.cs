using System.Globalization;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// Reads static-mesh instances from a Vision <c>.vzone</c> shapes archive.
/// </summary>
internal static class VZoneReader
{
    public static IReadOnlyList<StaticMeshInstance> Load(string path)
    {
        var data = File.ReadAllBytes(path);
        var reader = new VArchiveReader(
            data,
            loadingVersion: 30,
            useObjectLengths: false,
            hasPerObjectRange: false,
            serializers: VArchiveSerializers.Build(),
            object3dClasses: VArchiveSerializers.Object3dClasses);

        var archiveVersion = reader.ReadInt32();
        reader.LoadingVersion = archiveVersion;
        var zoneVersion = reader.ReadInt32();
        if (zoneVersion >= 10)
        {
            _ = reader.ReadDouble();
            _ = reader.ReadDouble();
            _ = reader.ReadDouble();
        }

        _ = reader.ReadInt32(); // object count
        _ = reader.ReadInt32(); // non-null count
        var rootCount = reader.ReadInt32();
        _ = reader.ReadInt32(); // reserved

        for (var i = 0; i < rootCount && !reader.Eof; i++)
        {
            _ = reader.ReadObject();
        }

        if (reader.StaticMeshes.Count == 0)
        {
            throw new InvalidDataException(
                string.Format(CultureInfo.InvariantCulture, "No static meshes in {0}.", path));
        }

        return reader.StaticMeshes;
    }
}

/// <summary>
/// Finds the <c>.vscene</c> for a batch name and lists its zone sidecar files.
/// </summary>
internal static class VSceneLocator
{
    public static string? FindScenePath(string scenesRoot, string batchName)
    {
        var expected = batchName + ".vscene";
        foreach (var path in Directory.EnumerateFiles(scenesRoot, expected, SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(path);
            var parentName = Path.GetFileName(Path.GetDirectoryName(path));
            if (string.Equals(fileName, expected, StringComparison.OrdinalIgnoreCase)
                && string.Equals(parentName, batchName, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return null;
    }

    public static IReadOnlyList<string> ZoneFilesForScene(string scenePath)
    {
        var dataDir = scenePath + "_data";
        if (!Directory.Exists(dataDir))
        {
            return [];
        }

        return Directory.GetFiles(dataDir, "*.vzone", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
