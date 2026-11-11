using System.Globalization;
using System.Numerics;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Content.Tool.Rendering;
using OpenWorker.Content.Tool.Vision;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Loads Vision scene static meshes for a batch map and merges them into one translucent draw list.
/// </summary>
internal static class SceneCatalog
{
    private static readonly Vector4 VisibleColor = new(0.62f, 0.64f, 0.68f, 0.20f);
    private static readonly Vector4 CollisionColor = new(0.75f, 0.45f, 0.30f, 0.12f);

    public static string? FindScenePath(string worldFolder, string batchName)
    {
        var scenes = Path.Combine(worldFolder, "Scenes");
        if (!Directory.Exists(scenes))
        {
            return null;
        }

        return VSceneLocator.FindScenePath(scenes, batchName);
    }

    public static SceneMeshDraw Load(string worldFolder, string scenePath)
    {
        var instances = new List<StaticMeshInstance>();
        foreach (var zonePath in VSceneLocator.ZoneFilesForScene(scenePath))
        {
            try
            {
                instances.AddRange(VZoneReader.Load(zonePath));
            }
            catch (Exception exception)
            {
                throw new InvalidDataException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Failed to read {0}: {1}",
                        Path.GetFileName(zonePath),
                        exception.Message),
                    exception);
            }
        }

        if (instances.Count == 0)
        {
            return new SceneMeshDraw
            {
                Faces = [],
                Minimum = Vector3.Zero,
                Maximum = Vector3.Zero,
                TriangleCount = 0,
                InstanceCount = 0,
                MeshCount = 0
            };
        }

        var meshCache = new Dictionary<string, VMeshReader.MeshGeometry>(StringComparer.OrdinalIgnoreCase);
        var faces = new List<float>(1 << 20);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var triangleCount = 0;
        var usedMeshes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var instance in instances)
        {
            if (string.IsNullOrWhiteSpace(instance.Path))
            {
                continue;
            }

            var resolved = ResolveMeshPath(worldFolder, instance.Path);
            if (resolved is null)
            {
                continue;
            }

            if (!meshCache.TryGetValue(resolved, out var geometry))
            {
                geometry = VMeshReader.Load(resolved);
                meshCache[resolved] = geometry;
            }

            usedMeshes.Add(resolved);
            var color = instance.IsCollisionOnly ? CollisionColor : VisibleColor;
            var matrix = instance.Matrix;

            for (var i = 0; i + 2 < geometry.Indices.Length; i += 3)
            {
                var a = Transform(geometry.Positions[geometry.Indices[i]], matrix);
                var b = Transform(geometry.Positions[geometry.Indices[i + 1]], matrix);
                var c = Transform(geometry.Positions[geometry.Indices[i + 2]], matrix);
                AddVertex(faces, a, color, ref min, ref max);
                AddVertex(faces, b, color, ref min, ref max);
                AddVertex(faces, c, color, ref min, ref max);
                triangleCount++;
            }
        }

        if (triangleCount == 0)
        {
            min = Vector3.Zero;
            max = Vector3.Zero;
        }

        return new SceneMeshDraw
        {
            Faces = [.. faces],
            Minimum = min,
            Maximum = max,
            TriangleCount = triangleCount,
            InstanceCount = instances.Count,
            MeshCount = usedMeshes.Count
        };
    }

    private static string? ResolveMeshPath(string worldFolder, string relativePath)
    {
        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        // Paths are rooted at the client datas folder (parent of World/).
        var datasRoot = Path.GetFullPath(Path.Combine(worldFolder, ".."));
        var candidate = Path.GetFullPath(Path.Combine(datasRoot, normalized));
        if (File.Exists(candidate))
        {
            return candidate;
        }

        candidate = Path.GetFullPath(Path.Combine(worldFolder, normalized));
        return File.Exists(candidate) ? candidate : null;
    }

    private static Vector3 Transform(Vector3 position, Matrix4x4 matrix)
    {
        // Vision stores rows with translation in column 3 (column-vector multiply).
        var game = new Vector3(
            matrix.M11 * position.X + matrix.M12 * position.Y + matrix.M13 * position.Z + matrix.M14,
            matrix.M21 * position.X + matrix.M22 * position.Y + matrix.M23 * position.Z + matrix.M24,
            matrix.M31 * position.X + matrix.M32 * position.Y + matrix.M33 * position.Z + matrix.M34);
        return GameCoordinates.ToGl(game);
    }

    private static void AddVertex(
        List<float> faces,
        Vector3 position,
        Vector4 color,
        ref Vector3 min,
        ref Vector3 max)
    {
        faces.Add(position.X);
        faces.Add(position.Y);
        faces.Add(position.Z);
        faces.Add(color.X);
        faces.Add(color.Y);
        faces.Add(color.Z);
        faces.Add(color.W);
        min = Vector3.Min(min, position);
        max = Vector3.Max(max, position);
    }
}
