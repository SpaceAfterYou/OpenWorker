using System.Numerics;

namespace OpenWorker.Navigation;

/// <summary>
/// The ground of one running map: the mesh that is the same for everybody and the boxes that close a way for now,
/// the gates that have not been opened yet. What is asked is asked of the ground as it is at the moment.
/// </summary>
public sealed class NavField
{
    /// <summary>
    /// How high above its feet a creature looks for the floor, in the units of the world. A creature on a staircase
    /// is lower than the step above it, and the step above it is not the floor.
    /// </summary>
    public const float FloorAbove = 200f;

    /// <summary>
    /// How far under its feet a creature looks for the floor.
    /// </summary>
    public const float FloorBelow = 1000f;

    private const float Margin = 150f;
    private const float MinimumWay = 1f;
    private const int MaximumSteps = 4096;

    private readonly List<Blocker> _blockers = [];

    public NavField(NavMesh mesh)
    {
        Mesh = mesh;
    }

    public NavMesh Mesh { get; }

    // ---- Boxes that close a way ---------------------------------------------------------------

    /// <summary>
    /// Adds a box that nothing can walk through while it is switched on.
    /// </summary>
    /// <param name="floor">The middle of the bottom of the box.</param>
    /// <param name="size">Width, depth and height of the box.</param>
    /// <param name="degrees">How the box is turned around the vertical axis.</param>
    /// <returns>The number to switch the box with.</returns>
    public int AddBlocker(Vector3 floor, Vector3 size, float degrees, bool enabled)
    {
        var angle = degrees * MathF.PI / 180f;

        _blockers.Add(new Blocker(
            new Vector2(floor.X, floor.Y),
            new Vector2(Math.Abs(size.X) / 2f, Math.Abs(size.Y) / 2f),
            MathF.Cos(angle),
            MathF.Sin(angle),
            floor.Z,
            floor.Z + Math.Abs(size.Z),
            enabled));

        return _blockers.Count - 1;
    }

    public void SetBlocker(int blocker, bool enabled)
    {
        if ((uint)blocker < (uint)_blockers.Count)
        {
            _blockers[blocker].Enabled = enabled;
        }
    }

    private sealed class Blocker(Vector2 center, Vector2 half, float cos, float sin, float bottom, float top, bool enabled)
    {
        public bool Enabled { get; set; } = enabled;

        public bool Contains(Vector3 point)
        {
            if (point.Z < bottom - Margin || point.Z > top + Margin)
            {
                return false;
            }

            var local = ToLocal(new Vector2(point.X, point.Y));

            return MathF.Abs(local.X) <= half.X && MathF.Abs(local.Y) <= half.Y;
        }

        /// <summary>
        /// The part of a line that is inside the box, as a part of the line from 0 to 1.
        /// </summary>
        public bool Clip(Vector3 from, Vector3 to, out float enter, out float leave)
        {
            enter = 0f;
            leave = 1f;

            if (Math.Max(from.Z, to.Z) < bottom - Margin || Math.Min(from.Z, to.Z) > top + Margin)
            {
                return false;
            }

            var start = ToLocal(new Vector2(from.X, from.Y));
            var end = ToLocal(new Vector2(to.X, to.Y));

            return ClipAxis(start.X, end.X, half.X, ref enter, ref leave) && ClipAxis(start.Y, end.Y, half.Y, ref enter, ref leave);
        }

        private static bool ClipAxis(float start, float end, float half, ref float enter, ref float leave)
        {
            var delta = end - start;

            if (MathF.Abs(delta) < 1e-6f)
            {
                return MathF.Abs(start) <= half;
            }

            var first = (-half - start) / delta;
            var second = (half - start) / delta;

            if (first > second)
            {
                (first, second) = (second, first);
            }

            enter = Math.Max(enter, first);
            leave = Math.Min(leave, second);

            return enter <= leave;
        }

        private Vector2 ToLocal(Vector2 point)
        {
            var offset = point - center;

            return new Vector2(offset.X * cos + offset.Y * sin, -offset.X * sin + offset.Y * cos);
        }
    }

    /// <summary>
    /// How much of a line can be walked before a box stops it, from 0 to 1.
    /// </summary>
    private float FreeFraction(Vector3 from, Vector3 to)
    {
        var free = 1f;

        foreach (var blocker in _blockers)
        {
            if (blocker.Enabled && blocker.Clip(from, to, out var enter, out _))
            {
                free = Math.Min(free, enter);
            }
        }

        return free;
    }

    /// <summary>
    /// What is left of a piece of a line, usually of an edge between faces, once the boxes are taken off it: its longest
    /// piece that is free. Nothing is left when the boxes close it.
    /// </summary>
    private bool TryFreePiece(Vector3 from, Vector3 to, out Vector3 freeFrom, out Vector3 freeTo)
    {
        freeFrom = from;
        freeTo = to;

        List<(float Enter, float Leave)>? closed = null;

        foreach (var blocker in _blockers)
        {
            if (blocker.Enabled && blocker.Clip(from, to, out var enter, out var leave))
            {
                (closed ??= []).Add((enter, leave));
            }
        }

        if (closed is null)
        {
            return true;
        }

        closed.Sort();

        var bestStart = 0f;
        var bestEnd = 0f;
        var cursor = 0f;

        foreach (var (enter, leave) in closed)
        {
            if (enter - cursor > bestEnd - bestStart)
            {
                bestStart = cursor;
                bestEnd = enter;
            }

            cursor = Math.Max(cursor, leave);
        }

        if (1f - cursor > bestEnd - bestStart)
        {
            bestStart = cursor;
            bestEnd = 1f;
        }

        if ((bestEnd - bestStart) * Vector3.Distance(from, to) < MinimumWay)
        {
            return false;
        }

        freeFrom = Vector3.Lerp(from, to, bestStart);
        freeTo = Vector3.Lerp(from, to, bestEnd);

        return true;
    }

    private bool IsClosed(Vector3 point)
    {
        foreach (var blocker in _blockers)
        {
            if (blocker.Enabled && blocker.Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    // ---- The floor ----------------------------------------------------------------------------

    /// <summary>
    /// The height of the floor under a point, as far as a creature standing there would look for it.
    /// </summary>
    public bool TryGetFloor(Vector3 position, out float height)
    {
        return Mesh.TryGetHeight(position, FloorAbove, FloorBelow, out height);
    }

    /// <summary>
    /// Whether a creature of a radius can stand on a point: there is a floor at the point and at the four places at the
    /// distance of the radius from it, in the directions of the axes.
    /// </summary>
    public bool IsWalkable(Vector3 position, float radius)
    {
        return TryGetFloor(position, out _) &&
               TryGetFloor(position with { X = position.X - radius }, out _) &&
               TryGetFloor(position with { X = position.X + radius }, out _) &&
               TryGetFloor(position with { Y = position.Y - radius }, out _) &&
               TryGetFloor(position with { Y = position.Y + radius }, out _);
    }

    /// <summary>
    /// The place of the ground a creature at a point stands on: the floor under it, or the nearest ground within the reach
    /// when it is off the ground a little.
    /// </summary>
    private bool TrySnap(Vector3 position, float reach, out NavPoint point)
    {
        if (Mesh.TryGetFloor(position, FloorAbove, FloorBelow, out var height, out var face))
        {
            point = new NavPoint(face, position with { Z = height });

            return true;
        }

        return Mesh.TryFindNearest(position, reach, out point);
    }

    // ---- Walking in a line --------------------------------------------------------------------

    /// <summary>
    /// Takes a creature that sets off in a straight line as far as it gets: to the end of the line, or to the wall (or the
    /// closed gate) it runs into, a radius before it. The height is that of the floor. The creature that does not stand on
    /// the ground stays where it is.
    /// </summary>
    public Vector3 Clamp(Vector3 from, Vector3 to, float radius, out bool reached)
    {
        reached = false;

        if (!TrySnap(from, Math.Max(radius, 1f) + 10f, out var start))
        {
            return from;
        }

        var begin = new Vector2(start.Position.X, start.Position.Y);
        var end = new Vector2(to.X, to.Y);
        var length = Vector2.Distance(begin, end);

        if (length < 1e-3f)
        {
            reached = true;

            return from;
        }

        var walk = Mesh.Walk(start.Face, begin, end, CanCross);
        var free = FreeFraction(start.Position, to);
        var fraction = Math.Min(walk.Fraction, free);

        if (walk.Reached && free >= 1f)
        {
            reached = true;

            return Mesh.TryGetFaceHeight(walk.Face, end, out var z) ? new Vector3(to.X, to.Y, z) : to;
        }

        var back = Math.Max(0f, fraction * length - radius);
        var stop = begin + (end - begin) / length * back;
        var point = new Vector3(stop.X, stop.Y, from.Z);

        return Mesh.TryGetHeight(point, FloorAbove, FloorBelow, out var floor) ? point with { Z = floor } : from;
    }

    /// <summary>
    /// Whether something at one place notices something at another: the straight line between them runs over the ground,
    /// without a wall, a pit or a closed gate on it.
    /// </summary>
    public bool CanSee(Vector3 from, Vector3 to)
    {
        Clamp(from, to, 0f, out var reached);

        return reached;
    }

    private bool CanCross(int edge)
    {
        if (_blockers.Count == 0)
        {
            return true;
        }

        var info = Mesh.GetEdge(edge);

        return TryFreePiece(Mesh.GetVertex(info.From), Mesh.GetVertex(info.To), out _, out _);
    }

    // ---- Looking for a way --------------------------------------------------------------------

    /// <summary>
    /// The way from one place to another, a point at the start, a point at every turn and a point at the end. The ends are the
    /// places of the ground nearest to the points, if there is some within the radius of the creature and a little more:
    /// a creature that is farther from the ground has no way.
    /// </summary>
    public bool TryFindPath(Vector3 from, Vector3 to, float radius, List<Vector3> path)
    {
        path.Clear();

        var reach = Math.Max(radius, 1f) + 10f;

        if (!TrySnap(from, reach, out var start) || !TrySnap(to, reach, out var goal))
        {
            return false;
        }

        if (start.Face == goal.Face)
        {
            if (FreeFraction(start.Position, goal.Position) < 1f)
            {
                return false;
            }

            path.Add(start.Position);
            path.Add(goal.Position);

            return true;
        }

        var faces = SearchFaces(start, goal, out var edges);

        if (faces is null)
        {
            return false;
        }

        Pull(start.Position, goal.Position, edges, radius, path);

        for (var i = 1; i < path.Count; i++)
        {
            if (FreeFraction(path[i - 1], path[i]) < 1f)
            {
                path.Clear();

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The faces on the way and the edges the way leaves them by, found by looking at the faces nearest to the goal first.
    /// </summary>
    private List<int>? SearchFaces(NavPoint start, NavPoint goal, out List<int> edges)
    {
        var count = Mesh.FaceCount;
        var cost = new float[count];
        var parent = new int[count];
        var through = new int[count];
        var entry = new Vector3[count];
        var done = new bool[count];
        var open = new PriorityQueue<int, float>();

        Array.Fill(cost, float.MaxValue);
        Array.Fill(parent, -1);
        Array.Fill(through, -1);

        cost[start.Face] = 0f;
        entry[start.Face] = start.Position;
        open.Enqueue(start.Face, 0f);

        edges = [];

        for (var step = 0; step < MaximumSteps && open.TryDequeue(out var face, out _); step++)
        {
            if (done[face])
            {
                continue;
            }

            done[face] = true;

            if (face == goal.Face)
            {
                var faces = new List<int> { face };

                for (var at = face; parent[at] >= 0; at = parent[at])
                {
                    edges.Add(through[at]);
                    faces.Add(parent[at]);
                }

                faces.Reverse();
                edges.Reverse();

                return faces;
            }

            var info = Mesh.GetFace(face);

            for (var i = 0; i < info.EdgeCount; i++)
            {
                var edgeIndex = info.FirstEdge + i;
                var edge = Mesh.GetEdge(edgeIndex);

                if (edge.OppositeFace < 0 ||
                    !TryFreePiece(Mesh.GetVertex(edge.From), Mesh.GetVertex(edge.To), out var left, out var right))
                {
                    continue;
                }

                var middle = (left + right) / 2f;

                // A gate can stand in the middle of a face, away from its edges.
                if (FreeFraction(entry[face], middle) < 1f ||
                    edge.OppositeFace == goal.Face && FreeFraction(middle, goal.Position) < 1f)
                {
                    continue;
                }

                var price = cost[face] + Vector3.Distance(entry[face], middle);

                if (price < cost[edge.OppositeFace])
                {
                    cost[edge.OppositeFace] = price;
                    parent[edge.OppositeFace] = face;
                    through[edge.OppositeFace] = edgeIndex;
                    entry[edge.OppositeFace] = middle;

                    open.Enqueue(edge.OppositeFace, price + Vector3.Distance(middle, goal.Position));
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Pulls a string through the edges the way crosses, from the start to the goal: it bends at the ends of the edges only
    /// where it has to. A bend is then moved a radius away from the end of the edge it bends on.
    /// </summary>
    private void Pull(Vector3 start, Vector3 goal, List<int> edges, float radius, List<Vector3> path)
    {
        var lefts = new List<Vector3> { start };
        var rights = new List<Vector3> { start };

        foreach (var edgeIndex in edges)
        {
            var edge = Mesh.GetEdge(edgeIndex);

            // Going out through an edge the face is on the left of it, so its end is on the left and its start on the right.
            if (!TryFreePiece(Mesh.GetVertex(edge.To), Mesh.GetVertex(edge.From), out var left, out var right))
            {
                left = Mesh.GetVertex(edge.To);
                right = Mesh.GetVertex(edge.From);
            }

            lefts.Add(left);
            rights.Add(right);
        }

        lefts.Add(goal);
        rights.Add(goal);

        path.Add(start);

        var apex = start;
        var left0 = start;
        var right0 = start;
        var apexIndex = 0;
        var leftIndex = 0;
        var rightIndex = 0;

        for (var i = 1; i < lefts.Count; i++)
        {
            var left1 = lefts[i];
            var right1 = rights[i];

            if (Cross(apex, right0, right1) >= 0f)
            {
                if (SameSpot(apex, right0) || Cross(apex, left0, right1) < 0f)
                {
                    right0 = right1;
                    rightIndex = i;
                }
                else
                {
                    path.Add(left0);

                    apex = left0;
                    apexIndex = leftIndex;
                    left0 = right0 = apex;
                    leftIndex = rightIndex = apexIndex;
                    i = apexIndex;

                    continue;
                }
            }

            if (Cross(apex, left0, left1) <= 0f)
            {
                if (SameSpot(apex, left0) || Cross(apex, right0, left1) > 0f)
                {
                    left0 = left1;
                    leftIndex = i;
                }
                else
                {
                    path.Add(right0);

                    apex = right0;
                    apexIndex = rightIndex;
                    left0 = right0 = apex;
                    leftIndex = rightIndex = apexIndex;
                    i = apexIndex;
                }
            }
        }

        path.Add(goal);

        KeepAway(path, radius);

        // Points that lie on one another are one point.
        for (var i = path.Count - 1; i > 0; i--)
        {
            if (Vector3.DistanceSquared(path[i], path[i - 1]) < 0.25f)
            {
                path.RemoveAt(i);
            }
        }

        if (path.Count == 1)
        {
            path.Add(goal);
        }
    }

    /// <summary>
    /// A creature that goes round the end of an edge does not rub against it: the bend is moved to the outside of the turn,
    /// as far as the radius if the ground is there.
    /// </summary>
    private void KeepAway(List<Vector3> path, float radius)
    {
        for (var i = 1; i < path.Count - 1; i++)
        {
            var before = path[i] - path[i - 1];
            var after = path[i + 1] - path[i];
            var shorter = Math.Min(before.Length(), after.Length());

            if (shorter < 1e-3f)
            {
                continue;
            }

            var inside = Vector3.Normalize(after) - Vector3.Normalize(before);
            inside.Z = 0f;

            if (inside.LengthSquared() < 1e-6f)
            {
                continue;
            }

            var outward = -Vector3.Normalize(inside);

            for (var reach = Math.Min(radius, shorter / 2f); reach > 1f; reach /= 2f)
            {
                var moved = path[i] + outward * reach;

                if (Mesh.TryFindNearest(moved, 1f, out var on))
                {
                    path[i] = on.Position;

                    break;
                }
            }
        }
    }

    private static bool SameSpot(Vector3 first, Vector3 second)
    {
        return MathF.Abs(first.X - second.X) < 1e-3f && MathF.Abs(first.Y - second.Y) < 1e-3f;
    }

    /// <summary>
    /// Positive when the third point is on the left of the line from the first point to the second one.
    /// </summary>
    private static float Cross(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }
}
