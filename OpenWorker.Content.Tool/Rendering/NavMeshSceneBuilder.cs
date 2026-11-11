using System.Numerics;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Navigation;

namespace OpenWorker.Content.Tool.Rendering;

/// <summary>
/// Builds an <see cref="OrbitScene"/> from a navigation mesh parsed from a Havok tagfile.
/// </summary>
internal static class NavMeshSceneBuilder
{
    private const float FaceAlpha = 0.32f;
    private const float AxisLengthFactor = 0.08f;
    private const int GridLines = 20;

    public static OrbitScene Build(NavMesh mesh)
    {
        var faces = new List<float>(mesh.FaceCount * 18 * 7);
        var lines = new List<float>(mesh.EdgeCount * 2 * 7);

        void AddVertex(List<float> sink, Vector3 position, Vector4 color)
        {
            sink.Add(position.X);
            sink.Add(position.Y);
            sink.Add(position.Z);
            sink.Add(color.X);
            sink.Add(color.Y);
            sink.Add(color.Z);
            sink.Add(color.W);
        }

        void AddLine(Vector3 a, Vector3 b, Vector4 color)
        {
            AddVertex(lines, a, color);
            AddVertex(lines, b, color);
        }

        void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector4 color)
        {
            AddVertex(faces, a, color);
            AddVertex(faces, b, color);
            AddVertex(faces, c, color);
        }

        var min = GameCoordinates.ToGl(mesh.Minimum);
        var max = GameCoordinates.ToGl(mesh.Maximum);
        var origin = (min + max) * 0.5f;
        var ground = MathF.Max(max.X - min.X, max.Z - min.Z);
        var gridHalfExtent = Math.Max(10f, ground * 0.55f);
        var gridStep = NiceStep(gridHalfExtent / GridLines);

        var gridColor = new Vector4(0.35f, 0.37f, 0.42f, 1f);
        var y = origin.Y;
        for (var i = -gridHalfExtent; i <= gridHalfExtent + gridStep * 0.01f; i += gridStep)
        {
            AddLine(
                new Vector3(origin.X + i, y, origin.Z - gridHalfExtent),
                new Vector3(origin.X + i, y, origin.Z + gridHalfExtent),
                gridColor);
            AddLine(
                new Vector3(origin.X - gridHalfExtent, y, origin.Z + i),
                new Vector3(origin.X + gridHalfExtent, y, origin.Z + i),
                gridColor);
        }

        var axis = Math.Max(gridStep * 2f, gridHalfExtent * AxisLengthFactor);
        AddLine(origin, origin + new Vector3(axis, 0f, 0f), new Vector4(0.9f, 0.2f, 0.2f, 1f));
        AddLine(origin, origin + new Vector3(0f, axis, 0f), new Vector4(0.2f, 0.85f, 0.3f, 1f));
        AddLine(origin, origin + new Vector3(0f, 0f, axis), new Vector4(0.25f, 0.45f, 0.95f, 1f));

        var walk = new Vector4(0.25f, 0.75f, 0.45f, FaceAlpha);
        var walkEdge = new Vector4(0.35f, 0.85f, 0.55f, 1f);
        var wallEdge = new Vector4(0.95f, 0.45f, 0.25f, 1f);

        for (var face = 0; face < mesh.FaceCount; face++)
        {
            var corners = mesh.GetCorners(face).Select(GameCoordinates.ToGl).ToArray();
            if (corners.Length < 3)
            {
                continue;
            }

            for (var i = 1; i < corners.Length - 1; i++)
            {
                AddTriangle(corners[0], corners[i], corners[i + 1], walk);
            }
        }

        for (var edgeIndex = 0; edgeIndex < mesh.EdgeCount; edgeIndex++)
        {
            var edge = mesh.GetEdge(edgeIndex);
            var from = GameCoordinates.ToGl(mesh.GetVertex(edge.From));
            var to = GameCoordinates.ToGl(mesh.GetVertex(edge.To));
            AddLine(from, to, edge.OppositeFace < 0 ? wallEdge : walkEdge);
        }

        return new OrbitScene
        {
            Faces = [.. faces],
            Lines = [.. lines],
            Minimum = Vector3.Min(min, max),
            Maximum = Vector3.Max(min, max)
        };
    }

    private static float NiceStep(float rough)
    {
        if (rough <= 0f)
        {
            return 1f;
        }

        var exponent = MathF.Floor(MathF.Log10(rough));
        var baseStep = MathF.Pow(10f, exponent);
        var fraction = rough / baseStep;
        var nice = fraction <= 1f ? 1f : fraction <= 2f ? 2f : fraction <= 5f ? 5f : 10f;
        return nice * baseStep;
    }
}
