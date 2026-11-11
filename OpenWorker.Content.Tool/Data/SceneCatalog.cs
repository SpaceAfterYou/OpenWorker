using System.Globalization;
using System.Numerics;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Content.Tool.Rendering;
using OpenWorker.Content.Tool.Vision;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Loads Vision scene static meshes for a batch map, one layer per <c>.vzone</c>.
/// </summary>
internal static class SceneCatalog
{
    private static readonly Vector4 VisibleColor = new(0.62f, 0.64f, 0.68f, 0.20f);
    private static readonly Vector4 CollisionColor = new(0.75f, 0.45f, 0.30f, 0.12f);
    private static readonly Vector4 TexturedTint = new(1f, 1f, 1f, 1f);

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
        var datasRoot = Path.GetFullPath(Path.Combine(worldFolder, ".."));
        var meshCache = new Dictionary<string, VMeshReader.MeshGeometry>(StringComparer.OrdinalIgnoreCase);
        var diffuseCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var zones = new List<SceneZoneLayer>();
        var allColorFaces = new List<float>(1 << 20);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var triangleCount = 0;
        var instanceCount = 0;
        var usedMeshes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var zonePath in VSceneLocator.ZoneFilesForScene(scenePath))
        {
            IReadOnlyList<StaticMeshInstance> instances;
            try
            {
                instances = VZoneReader.Load(zonePath);
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

            var zoneColor = new List<float>(1 << 16);
            var batchesByTexture = new Dictionary<string, List<float>>(StringComparer.OrdinalIgnoreCase);
            var untextured = new List<float>();
            var zoneTriangles = 0;
            var zoneMin = new Vector3(float.MaxValue);
            var zoneMax = new Vector3(float.MinValue);

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

                if (!diffuseCache.TryGetValue(resolved, out var diffuse))
                {
                    diffuse = MeshMaterialLookup.FindDiffusePath(resolved, datasRoot);
                    diffuseCache[resolved] = diffuse;
                }

                usedMeshes.Add(resolved);
                var color = instance.IsCollisionOnly ? CollisionColor : VisibleColor;
                var matrix = instance.Matrix;
                var useTexture = !instance.IsCollisionOnly && geometry.HasUv && diffuse is not null;
                var texturedSink = useTexture
                    ? GetOrCreateBatch(batchesByTexture, diffuse!)
                    : null;

                for (var i = 0; i + 2 < geometry.Indices.Length; i += 3)
                {
                    var ia = geometry.Indices[i];
                    var ib = geometry.Indices[i + 1];
                    var ic = geometry.Indices[i + 2];
                    var a = Transform(geometry.Positions[ia], matrix);
                    var b = Transform(geometry.Positions[ib], matrix);
                    var c = Transform(geometry.Positions[ic], matrix);

                    AddColorVertex(zoneColor, a, color, ref zoneMin, ref zoneMax);
                    AddColorVertex(zoneColor, b, color, ref zoneMin, ref zoneMax);
                    AddColorVertex(zoneColor, c, color, ref zoneMin, ref zoneMax);

                    if (texturedSink is not null)
                    {
                        AddTexturedVertex(texturedSink, a, geometry.Uvs[ia]);
                        AddTexturedVertex(texturedSink, b, geometry.Uvs[ib]);
                        AddTexturedVertex(texturedSink, c, geometry.Uvs[ic]);
                    }
                    else if (!instance.IsCollisionOnly)
                    {
                        AddColorVertex(untextured, a, TexturedTint, ref zoneMin, ref zoneMax);
                        AddColorVertex(untextured, b, TexturedTint, ref zoneMin, ref zoneMax);
                        AddColorVertex(untextured, c, TexturedTint, ref zoneMin, ref zoneMax);
                    }

                    zoneTriangles++;
                }
            }

            if (zoneTriangles == 0)
            {
                continue;
            }

            var batches = new List<SceneTexturedBatch>(batchesByTexture.Count + 1);
            foreach (var (texturePath, vertices) in batchesByTexture)
            {
                batches.Add(new SceneTexturedBatch
                {
                    TexturePath = texturePath,
                    Vertices = [.. vertices],
                    TriangleCount = vertices.Count / 15
                });
            }

            if (untextured.Count > 0)
            {
                // Solid white stand-in when UVs or a diffuse map are missing.
                var solid = new List<float>(untextured.Count / 7 * 5);
                for (var i = 0; i + 6 < untextured.Count; i += 7)
                {
                    solid.Add(untextured[i]);
                    solid.Add(untextured[i + 1]);
                    solid.Add(untextured[i + 2]);
                    solid.Add(0f);
                    solid.Add(0f);
                }

                batches.Add(new SceneTexturedBatch
                {
                    TexturePath = null,
                    Vertices = [.. solid],
                    TriangleCount = solid.Count / 15
                });
            }

            var zoneName = Path.GetFileNameWithoutExtension(zonePath);
            zones.Add(new SceneZoneLayer
            {
                Name = zoneName,
                ColorFaces = [.. zoneColor],
                TexturedBatches = batches,
                TriangleCount = zoneTriangles,
                InstanceCount = instances.Count,
                IsVisible = true
            });

            allColorFaces.AddRange(zoneColor);
            min = Vector3.Min(min, zoneMin);
            max = Vector3.Max(max, zoneMax);
            triangleCount += zoneTriangles;
            instanceCount += instances.Count;
        }

        if (triangleCount == 0)
        {
            min = Vector3.Zero;
            max = Vector3.Zero;
        }

        return new SceneMeshDraw
        {
            Zones = zones,
            Faces = [.. allColorFaces],
            Minimum = min,
            Maximum = max,
            TriangleCount = triangleCount,
            InstanceCount = instanceCount,
            MeshCount = usedMeshes.Count
        };
    }

    private static List<float> GetOrCreateBatch(Dictionary<string, List<float>> map, string texturePath)
    {
        if (!map.TryGetValue(texturePath, out var list))
        {
            list = new List<float>(1 << 14);
            map[texturePath] = list;
        }

        return list;
    }

    private static string? ResolveMeshPath(string worldFolder, string relativePath)
    {
        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

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
        var game = new Vector3(
            matrix.M11 * position.X + matrix.M12 * position.Y + matrix.M13 * position.Z + matrix.M14,
            matrix.M21 * position.X + matrix.M22 * position.Y + matrix.M23 * position.Z + matrix.M24,
            matrix.M31 * position.X + matrix.M32 * position.Y + matrix.M33 * position.Z + matrix.M34);
        return GameCoordinates.ToGl(game);
    }

    private static void AddColorVertex(
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

    private static void AddTexturedVertex(List<float> faces, Vector3 position, Vector2 uv)
    {
        faces.Add(position.X);
        faces.Add(position.Y);
        faces.Add(position.Z);
        faces.Add(uv.X);
        faces.Add(uv.Y);
    }
}
