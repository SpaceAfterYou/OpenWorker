using System.Numerics;
using OpenWorker.Navigation.Havok;

namespace OpenWorker.Navigation;

/// <summary>
/// One edge of a face. The edges of a face follow one another counterclockwise seen from above, so the inside of the
/// face is on the left of an edge.
/// </summary>
/// <param name="From">The vertex the edge starts at.</param>
/// <param name="To">The vertex the edge ends at, where the next edge starts.</param>
/// <param name="OppositeEdge">The same edge in the neighbour face, or -1 where there is no neighbour: that is a wall or an edge of the ground.</param>
/// <param name="OppositeFace">The neighbour face, or -1.</param>
public readonly record struct NavEdge(int From, int To, int OppositeEdge, int OppositeFace);

/// <summary>
/// A convex piece of the ground that can be walked, made of the edges from <paramref name="FirstEdge"/> on.
/// </summary>
public readonly record struct NavFace(int FirstEdge, int EdgeCount);

/// <summary>
/// A place on the ground: a face and the point in it.
/// </summary>
public readonly record struct NavPoint(int Face, Vector3 Position);

/// <summary>
/// Where a walk along a straight line through the faces ended.
/// </summary>
/// <param name="Reached">The walk got to the end, nothing stood in the way.</param>
/// <param name="Fraction">How much of the line was walked, from 0 to 1.</param>
/// <param name="Face">The face the walk ended in.</param>
public readonly record struct NavWalk(bool Reached, float Fraction, int Face);

/// <summary>
/// The ground a creature can walk on, as the navigation mesh of a map gives it: convex faces that touch along edges.
/// The positions are in the units of the world, centimeters, with Z up. A face is a polygon that is flat to a fair degree,
/// so its height at a point is the one of the triangle of its fan that the point is in.
/// </summary>
public sealed class NavMesh
{
    /// <summary>
    /// The files keep meters, the world counts centimeters.
    /// </summary>
    public const float Scale = 100f;

    private const float CellSize = 500f;
    private const float Epsilon = 1e-4f;

    private readonly Vector3[] _vertices;
    private readonly NavFace[] _faces;
    private readonly NavEdge[] _edges;
    private readonly int[][] _faceVertices;
    private readonly FaceBounds[] _bounds;
    private readonly int[][] _cells;
    private readonly int _columns;
    private readonly int _rows;
    private readonly Vector2 _origin;

    private readonly record struct FaceBounds(float MinX, float MinY, float MaxX, float MaxY, float MinZ, float MaxZ);

    public NavMesh(IReadOnlyList<Vector3> vertices, IReadOnlyList<NavFace> faces, IReadOnlyList<NavEdge> edges)
    {
        _vertices = [.. vertices];
        _faces = [.. faces];
        _edges = [.. edges];

        _faceVertices = new int[_faces.Length][];
        _bounds = new FaceBounds[_faces.Length];

        var minimum = new Vector3(float.MaxValue);
        var maximum = new Vector3(float.MinValue);

        for (var i = 0; i < _faces.Length; i++)
        {
            var face = _faces[i];

            if (face.EdgeCount < 3 || face.FirstEdge < 0 || face.FirstEdge + face.EdgeCount > _edges.Length)
            {
                throw new InvalidDataException("A face of the navigation mesh has edges that are not there.");
            }

            var list = new int[face.EdgeCount];
            var bounds = new FaceBounds(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue, float.MaxValue, float.MinValue);

            for (var k = 0; k < list.Length; k++)
            {
                var edge = _edges[face.FirstEdge + k];

                if ((uint)edge.From >= (uint)_vertices.Length || (uint)edge.To >= (uint)_vertices.Length)
                {
                    throw new InvalidDataException("An edge of the navigation mesh has vertices that are not there.");
                }

                list[k] = edge.From;

                var vertex = _vertices[edge.From];

                bounds = new FaceBounds(
                    Math.Min(bounds.MinX, vertex.X),
                    Math.Min(bounds.MinY, vertex.Y),
                    Math.Max(bounds.MaxX, vertex.X),
                    Math.Max(bounds.MaxY, vertex.Y),
                    Math.Min(bounds.MinZ, vertex.Z),
                    Math.Max(bounds.MaxZ, vertex.Z));
            }

            _faceVertices[i] = list;
            _bounds[i] = bounds;

            minimum = Vector3.Min(minimum, new Vector3(bounds.MinX, bounds.MinY, bounds.MinZ));
            maximum = Vector3.Max(maximum, new Vector3(bounds.MaxX, bounds.MaxY, bounds.MaxZ));
        }

        if (_faces.Length == 0)
        {
            minimum = maximum = Vector3.Zero;
        }

        Minimum = minimum;
        Maximum = maximum;

        _origin = new Vector2(minimum.X, minimum.Y);
        _columns = Math.Max(1, (int)MathF.Ceiling((maximum.X - minimum.X) / CellSize) + 1);
        _rows = Math.Max(1, (int)MathF.Ceiling((maximum.Y - minimum.Y) / CellSize) + 1);

        var cells = new List<int>[_columns * _rows];

        for (var i = 0; i < _faces.Length; i++)
        {
            var bounds = _bounds[i];
            var (firstColumn, firstRow) = CellOf(bounds.MinX, bounds.MinY);
            var (lastColumn, lastRow) = CellOf(bounds.MaxX, bounds.MaxY);

            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var column = firstColumn; column <= lastColumn; column++)
                {
                    (cells[row * _columns + column] ??= []).Add(i);
                }
            }
        }

        _cells = [.. cells.Select(e => e?.ToArray() ?? [])];
    }

    /// <summary>
    /// The lowest and the highest corner of the ground.
    /// </summary>
    public Vector3 Minimum { get; }

    public Vector3 Maximum { get; }

    public int FaceCount => _faces.Length;

    public int EdgeCount => _edges.Length;

    public int VertexCount => _vertices.Length;

    public Vector3 GetVertex(int index) => _vertices[index];

    public NavFace GetFace(int face) => _faces[face];

    public NavEdge GetEdge(int edge) => _edges[edge];

    /// <summary>
    /// The corners of a face, counterclockwise.
    /// </summary>
    public IEnumerable<Vector3> GetCorners(int face) => _faceVertices[face].Select(e => _vertices[e]);

    public static NavMesh Load(string path)
    {
        return Parse(File.ReadAllBytes(path));
    }

    public static NavMesh Parse(ReadOnlySpan<byte> data)
    {
        var file = HavokTagfile.Parse(data);
        var mesh = file.Find("hkaiNavMesh") ?? throw new InvalidDataException("The file has no navigation mesh.");

        var vertices = mesh
            .GetArray<float[]>("vertices")
            .Select(e => e.Length >= 3 ? new Vector3(e[0], e[1], e[2]) * Scale : throw new InvalidDataException("A vertex of the navigation mesh is short."))
            .ToArray();

        var faces = mesh
            .GetArray<HavokObject>("faces")
            .Select(e => new NavFace((int)e.GetInt("startEdgeIndex"), (int)e.GetInt("numEdges")))
            .ToArray();

        var edges = mesh
            .GetArray<HavokObject>("edges")
            .Select(e => new NavEdge((int)e.GetInt("a"), (int)e.GetInt("b"), Index(e.GetInt("oppositeEdge", -1)), Index(e.GetInt("oppositeFace", -1))))
            .ToArray();

        foreach (var edge in edges)
        {
            if (edge.OppositeFace >= faces.Length || edge.OppositeEdge >= edges.Length)
            {
                throw new InvalidDataException("An edge of the navigation mesh has a neighbour that is not there.");
            }
        }

        return new NavMesh(vertices, faces, edges);

        // A neighbour that is not there is all ones in the file, whatever the sign.
        static int Index(long value) => value is < 0 or > int.MaxValue ? -1 : (int)value;
    }

    // ---- Where a point is ---------------------------------------------------------------------

    private (int Column, int Row) CellOf(float x, float y)
    {
        return (Math.Clamp((int)MathF.Floor((x - _origin.X) / CellSize), 0, _columns - 1),
            Math.Clamp((int)MathF.Floor((y - _origin.Y) / CellSize), 0, _rows - 1));
    }

    private IEnumerable<int> FacesNear(float minX, float minY, float maxX, float maxY)
    {
        if (_faces.Length == 0 || maxX < Minimum.X || minX > Maximum.X || maxY < Minimum.Y || minY > Maximum.Y)
        {
            yield break;
        }

        var (firstColumn, firstRow) = CellOf(minX, minY);
        var (lastColumn, lastRow) = CellOf(maxX, maxY);

        for (var row = firstRow; row <= lastRow; row++)
        {
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                foreach (var face in _cells[row * _columns + column])
                {
                    // A face that is in several cells is seen from the first of them only.
                    var bounds = _bounds[face];
                    var (cellColumn, cellRow) = CellOf(Math.Max(bounds.MinX, minX), Math.Max(bounds.MinY, minY));

                    if (cellColumn == column && cellRow == row)
                    {
                        yield return face;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Whether a point is above or below a face, ignoring how far.
    /// </summary>
    public bool Contains(int face, Vector2 point)
    {
        var bounds = _bounds[face];

        if (point.X < bounds.MinX - Epsilon || point.X > bounds.MaxX + Epsilon || point.Y < bounds.MinY - Epsilon || point.Y > bounds.MaxY + Epsilon)
        {
            return false;
        }

        var corners = _faceVertices[face];

        for (var i = 0; i < corners.Length; i++)
        {
            var from = _vertices[corners[i]];
            var to = _vertices[corners[(i + 1) % corners.Length]];
            var side = (to.X - from.X) * (point.Y - from.Y) - (to.Y - from.Y) * (point.X - from.X);
            var length = MathF.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y));

            // A distance outside the edge that is not worth a thought: a point on the edge belongs to both its faces.
            if (side < -0.01f * Math.Max(length, 1f))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The height of a face at a point above or below it.
    /// </summary>
    public bool TryGetFaceHeight(int face, Vector2 point, out float height)
    {
        var corners = _faceVertices[face];
        var first = _vertices[corners[0]];

        for (var i = 1; i < corners.Length - 1; i++)
        {
            var second = _vertices[corners[i]];
            var third = _vertices[corners[i + 1]];

            if (TryBarycentric(first, second, third, point, out var u, out var v, out var w))
            {
                height = u * first.Z + v * second.Z + w * third.Z;

                return true;
            }
        }

        height = 0f;

        return false;
    }

    private static bool TryBarycentric(Vector3 a, Vector3 b, Vector3 c, Vector2 point, out float u, out float v, out float w)
    {
        var denominator = (b.Y - c.Y) * (a.X - c.X) + (c.X - b.X) * (a.Y - c.Y);

        u = v = w = 0f;

        if (MathF.Abs(denominator) < 1e-9f)
        {
            return false;
        }

        u = ((b.Y - c.Y) * (point.X - c.X) + (c.X - b.X) * (point.Y - c.Y)) / denominator;
        v = ((c.Y - a.Y) * (point.X - c.X) + (a.X - c.X) * (point.Y - c.Y)) / denominator;
        w = 1f - u - v;

        const float tolerance = -1e-4f;

        return u >= tolerance && v >= tolerance && w >= tolerance;
    }

    /// <summary>
    /// The floor under a point: the highest face that is no higher than <paramref name="above"/> over the point and no lower
    /// than <paramref name="below"/> under it.
    /// </summary>
    public bool TryGetHeight(Vector3 position, float above, float below, out float height)
    {
        return TryGetFloor(position, above, below, out height, out _);
    }

    public bool TryGetFloor(Vector3 position, float above, float below, out float height, out int face)
    {
        var xy = new Vector2(position.X, position.Y);
        var best = float.MinValue;
        var bestFace = -1;

        foreach (var candidate in FacesNear(position.X, position.Y, position.X, position.Y))
        {
            var bounds = _bounds[candidate];

            if (bounds.MaxZ < position.Z - below - 1f || bounds.MinZ > position.Z + above + 1f)
            {
                continue;
            }

            if (TryGetFaceHeight(candidate, xy, out var z) && z <= position.Z + above && z >= position.Z - below && z > best)
            {
                best = z;
                bestFace = candidate;
            }
        }

        height = bestFace < 0 ? 0f : best;
        face = bestFace;

        return bestFace >= 0;
    }

    /// <summary>
    /// The place of the ground nearest to a point, if it is no farther than the radius.
    /// </summary>
    public bool TryFindNearest(Vector3 position, float radius, out NavPoint nearest)
    {
        var best = radius * radius;
        var found = false;

        nearest = default;

        foreach (var face in FacesNear(position.X - radius, position.Y - radius, position.X + radius, position.Y + radius))
        {
            var bounds = _bounds[face];

            if (bounds.MaxZ < position.Z - radius || bounds.MinZ > position.Z + radius)
            {
                continue;
            }

            var closest = ClosestOnFace(face, position);
            var distance = Vector3.DistanceSquared(closest, position);

            if (distance <= best)
            {
                best = distance;
                found = true;
                nearest = new NavPoint(face, closest);
            }
        }

        return found;
    }

    private Vector3 ClosestOnFace(int face, Vector3 point)
    {
        var corners = _faceVertices[face];
        var first = _vertices[corners[0]];
        var best = float.MaxValue;
        var result = first;

        for (var i = 1; i < corners.Length - 1; i++)
        {
            var closest = ClosestOnTriangle(point, first, _vertices[corners[i]], _vertices[corners[i + 1]]);
            var distance = Vector3.DistanceSquared(closest, point);

            if (distance < best)
            {
                best = distance;
                result = closest;
            }
        }

        return result;
    }

    private static Vector3 ClosestOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        var ab = b - a;
        var ac = c - a;
        var ap = p - a;
        var d1 = Vector3.Dot(ab, ap);
        var d2 = Vector3.Dot(ac, ap);

        if (d1 <= 0f && d2 <= 0f)
        {
            return a;
        }

        var bp = p - b;
        var d3 = Vector3.Dot(ab, bp);
        var d4 = Vector3.Dot(ac, bp);

        if (d3 >= 0f && d4 <= d3)
        {
            return b;
        }

        var vc = d1 * d4 - d3 * d2;

        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            return a + ab * (d1 / (d1 - d3));
        }

        var cp = p - c;
        var d5 = Vector3.Dot(ab, cp);
        var d6 = Vector3.Dot(ac, cp);

        if (d6 >= 0f && d5 <= d6)
        {
            return c;
        }

        var vb = d5 * d2 - d1 * d6;

        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            return a + ac * (d2 / (d2 - d6));
        }

        var va = d3 * d6 - d5 * d4;

        if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
        {
            return b + (c - b) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
        }

        var sum = 1f / (va + vb + vc);

        return a + ab * (vb * sum) + ac * (vc * sum);
    }

    // ---- Walking in a line --------------------------------------------------------------------

    /// <summary>
    /// Walks from a point of a face towards another point along a straight line, seen from above, from face to face through
    /// the edges they share. The walk stops at an edge that has no neighbour, or that <paramref name="canCross"/> does not
    /// let through.
    /// </summary>
    public NavWalk Walk(int face, Vector2 from, Vector2 to, Func<int, bool>? canCross = null)
    {
        var direction = to - from;
        var entered = 0f;
        var cameBy = -1;

        for (var step = 0; step <= _faces.Length + 8; step++)
        {
            var info = _faces[face];
            var exit = float.MaxValue;

            for (var i = 0; i < info.EdgeCount; i++)
            {
                if (TryExitTime(info.FirstEdge + i, cameBy, from, direction, entered, out var time) && time < exit)
                {
                    exit = time;
                }
            }

            if (exit >= 1f)
            {
                return new NavWalk(true, 1f, face);
            }

            // At a corner, where the line leaves through two edges at once, the edge that leads on is the better one.
            var exitEdge = -1;

            for (var i = 0; i < info.EdgeCount; i++)
            {
                var edgeIndex = info.FirstEdge + i;

                if (!TryExitTime(edgeIndex, cameBy, from, direction, entered, out var time) || time > exit + 1e-5f)
                {
                    continue;
                }

                var leads = _edges[edgeIndex].OppositeFace >= 0 && (canCross is null || canCross(edgeIndex));

                if (exitEdge < 0 || leads && !(_edges[exitEdge].OppositeFace >= 0 && (canCross is null || canCross(exitEdge))))
                {
                    exitEdge = edgeIndex;
                }
            }

            var crossed = _edges[exitEdge];

            if (crossed.OppositeFace < 0 || canCross is not null && !canCross(exitEdge))
            {
                return new NavWalk(false, Math.Clamp(exit, 0f, 1f), face);
            }

            entered = exit;
            cameBy = crossed.OppositeEdge;
            face = crossed.OppositeFace;
        }

        return new NavWalk(false, entered, face);
    }

    /// <summary>
    /// When a line that goes out of a face crosses an edge of it, if it does at all.
    /// </summary>
    private bool TryExitTime(int edgeIndex, int skip, Vector2 from, Vector2 direction, float entered, out float time)
    {
        time = 0f;

        if (edgeIndex == skip)
        {
            return false;
        }

        var edge = _edges[edgeIndex];
        var start = _vertices[edge.From];
        var end = _vertices[edge.To];

        // The normal of an edge looks out of the face.
        var normal = new Vector2(end.Y - start.Y, start.X - end.X);
        var speed = Vector2.Dot(normal, direction);

        if (speed <= Epsilon * normal.Length())
        {
            return false;
        }

        time = Math.Max(Vector2.Dot(normal, new Vector2(start.X, start.Y) - from) / speed, entered);

        return true;
    }
}
