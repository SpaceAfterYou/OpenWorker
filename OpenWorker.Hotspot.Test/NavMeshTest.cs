using System.Numerics;
using OpenWorker.Navigation;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class NavMeshTest
{
    private static readonly string? Folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_NAVMESH");

    /// <summary>
    /// Squares of the ground that touch along the sides. A square is (column, row), a column is 1000 wide.
    /// </summary>
    internal static NavMesh Squares(params (int Column, int Row)[] squares)
    {
        var vertices = new List<Vector3>();
        var indices = new Dictionary<(int, int), int>();
        var polygons = new List<int[]>();

        int Vertex(int x, int y)
        {
            if (!indices.TryGetValue((x, y), out var index))
            {
                index = vertices.Count;
                indices[(x, y)] = index;
                vertices.Add(new Vector3(x * 1000f, y * 1000f, 0f));
            }

            return index;
        }

        foreach (var (column, row) in squares)
        {
            polygons.Add([Vertex(column, row), Vertex(column + 1, row), Vertex(column + 1, row + 1), Vertex(column, row + 1)]);
        }

        var faces = new List<NavFace>();
        var edges = new List<NavEdge>();
        var byEnds = new Dictionary<(int, int), (int Edge, int Face)>();

        for (var face = 0; face < polygons.Count; face++)
        {
            faces.Add(new NavFace(edges.Count, polygons[face].Length));

            for (var i = 0; i < polygons[face].Length; i++)
            {
                var from = polygons[face][i];
                var to = polygons[face][(i + 1) % polygons[face].Length];

                byEnds[(from, to)] = (edges.Count, face);
                edges.Add(new NavEdge(from, to, -1, -1));
            }
        }

        for (var i = 0; i < edges.Count; i++)
        {
            if (byEnds.TryGetValue((edges[i].To, edges[i].From), out var opposite))
            {
                edges[i] = edges[i] with { OppositeEdge = opposite.Edge, OppositeFace = opposite.Face };
            }
        }

        return new NavMesh(vertices, faces, edges);
    }

    /// <summary>
    /// A corridor in the shape of an L: three squares to the east and then three to the north.
    /// </summary>
    private static NavMesh Corner() => Squares((0, 0), (1, 0), (2, 0), (2, 1), (2, 2));

    // ---- The mesh ---------------------------------------------------------------------------

    [TestMethod]
    public void TheFloorIsUnderThePointAndNotUnderTheOutside()
    {
        var field = new NavField(Squares((0, 0), (1, 0)));

        Assert.IsTrue(field.TryGetFloor(new Vector3(500, 500, 30), out var height));
        Assert.AreEqual(0f, height);
        Assert.IsFalse(field.TryGetFloor(new Vector3(500, 1500, 0), out _));
        Assert.IsFalse(field.TryGetFloor(new Vector3(500, 500, 5000), out _), "The floor far under the creature is not its floor.");
    }

    [TestMethod]
    public void TheFloorUnderACreatureIsTheHighestOneBelowItsHead()
    {
        // Two floors, one above the other: the creature on the upper one stands on it and not on the lower one.
        var vertices = new[]
        {
            new Vector3(0, 0, 0), new Vector3(1000, 0, 0), new Vector3(1000, 1000, 0), new Vector3(0, 1000, 0),
            new Vector3(0, 0, 400), new Vector3(1000, 0, 400), new Vector3(1000, 1000, 400), new Vector3(0, 1000, 400)
        };

        NavEdge[] Edges(int first) =>
        [
            new(first, first + 1, -1, -1), new(first + 1, first + 2, -1, -1),
            new(first + 2, first + 3, -1, -1), new(first + 3, first, -1, -1)
        ];

        var mesh = new NavMesh(vertices, [new NavFace(0, 4), new NavFace(4, 4)], [.. Edges(0), .. Edges(4)]);
        var field = new NavField(mesh);

        Assert.IsTrue(field.TryGetFloor(new Vector3(500, 500, 400), out var upper));
        Assert.AreEqual(400f, upper);
        Assert.IsTrue(field.TryGetFloor(new Vector3(500, 500, 100), out var lower));
        Assert.AreEqual(0f, lower, "The floor above the head is out of reach.");
    }

    [TestMethod]
    public void ASlopeGivesTheHeightOfItsPlane()
    {
        var mesh = new NavMesh(
            [new Vector3(0, 0, 0), new Vector3(1000, 0, 0), new Vector3(1000, 1000, 500), new Vector3(0, 1000, 500)],
            [new NavFace(0, 4)],
            [new NavEdge(0, 1, -1, -1), new NavEdge(1, 2, -1, -1), new NavEdge(2, 3, -1, -1), new NavEdge(3, 0, -1, -1)]);

        Assert.IsTrue(mesh.TryGetHeight(new Vector3(300, 500, 400), 200f, 1000f, out var height));
        Assert.AreEqual(250f, height, 0.5f);
    }

    [TestMethod]
    public void TheNearestGroundIsFoundWithinTheRadius()
    {
        var mesh = Squares((0, 0));

        Assert.IsTrue(mesh.TryFindNearest(new Vector3(-40, 500, 0), 50f, out var near));
        Assert.AreEqual(0, near.Face);
        Assert.AreEqual(0f, near.Position.X, 0.01f);
        Assert.IsFalse(mesh.TryFindNearest(new Vector3(-60, 500, 0), 50f, out _));
    }

    [TestMethod]
    public void AWalkStopsWhereTheGroundEnds()
    {
        var mesh = Squares((0, 0), (1, 0));
        var start = new Vector2(500, 500);

        var reached = mesh.Walk(0, start, new Vector2(1500, 500));
        var stopped = mesh.Walk(0, start, new Vector2(2500, 500));

        Assert.IsTrue(reached.Reached);
        Assert.AreEqual(1, reached.Face);
        Assert.IsFalse(stopped.Reached);
        Assert.AreEqual(0.75f, stopped.Fraction, 0.001f);
    }

    [TestMethod]
    public void AWalkGoesRoundTheCornerOfAFaceThroughTheOneThatLeadsOn()
    {
        var mesh = Squares((0, 0), (1, 0), (0, 1));

        // Along the diagonal from the first square to the one over it.
        var walk = mesh.Walk(0, new Vector2(500, 500), new Vector2(500, 1500));

        Assert.IsTrue(walk.Reached);
        Assert.AreEqual(2, walk.Face);
    }

    // ---- Moving in a line -------------------------------------------------------------------

    [TestMethod]
    public void ACreatureStopsARadiusBeforeTheWall()
    {
        var field = new NavField(Squares((0, 0), (1, 0)));

        var end = field.Clamp(new Vector3(500, 500, 0), new Vector3(5000, 500, 0), 100f, out var reached);

        Assert.IsFalse(reached);
        Assert.AreEqual(1900f, end.X, 1f);
        Assert.AreEqual(500f, end.Y, 1f);
    }

    [TestMethod]
    public void ACreatureGetsToAPlaceThatIsOnTheGroundAndTakesItsHeight()
    {
        var mesh = new NavMesh(
            [new Vector3(0, 0, 0), new Vector3(2000, 0, 200), new Vector3(2000, 1000, 200), new Vector3(0, 1000, 0)],
            [new NavFace(0, 4)],
            [new NavEdge(0, 1, -1, -1), new NavEdge(1, 2, -1, -1), new NavEdge(2, 3, -1, -1), new NavEdge(3, 0, -1, -1)]);

        var end = new NavField(mesh).Clamp(new Vector3(100, 500, 10), new Vector3(1000, 500, 0), 50f, out var reached);

        Assert.IsTrue(reached);
        Assert.AreEqual(100f, end.Z, 0.5f);
    }

    [TestMethod]
    public void ACreatureOffTheGroundStaysWhereItIs()
    {
        var field = new NavField(Squares((0, 0)));
        var start = new Vector3(5000, 5000, 0);

        Assert.AreEqual(start, field.Clamp(start, new Vector3(500, 500, 0), 50f, out var reached));
        Assert.IsFalse(reached);
    }

    // ---- Looking for a way ------------------------------------------------------------------

    [TestMethod]
    public void ThereIsNoTurnOnAStraightWay()
    {
        var path = new List<Vector3>();

        Assert.IsTrue(new NavField(Corner()).TryFindPath(new Vector3(500, 500, 0), new Vector3(2500, 500, 0), 40f, path));
        Assert.HasCount(2, path);
    }

    [TestMethod]
    public void TheWayRoundACornerBendsAtTheCorner()
    {
        var path = new List<Vector3>();

        Assert.IsTrue(new NavField(Corner()).TryFindPath(new Vector3(500, 500, 0), new Vector3(2500, 2500, 0), 40f, path));
        Assert.HasCount(3, path);

        var corner = path[1];

        // The inner corner of the L is at (2000, 1000), and the way keeps a radius away from the other wall.
        Assert.IsGreaterThan(1900f, corner.X);
        Assert.IsGreaterThan(900f, corner.Y);
        Assert.AreEqual(new Vector3(500, 500, 0), path[0]);
        Assert.AreEqual(new Vector3(2500, 2500, 0), path[^1]);
    }

    [TestMethod]
    public void NoWayLeadsToAnIslandOrOffTheGround()
    {
        var field = new NavField(Squares((0, 0), (1, 0), (5, 5)));
        var path = new List<Vector3>();

        Assert.IsFalse(field.TryFindPath(new Vector3(500, 500, 0), new Vector3(5500, 5500, 0), 40f, path), "The island is not joined.");
        Assert.IsFalse(field.TryFindPath(new Vector3(500, 500, 0), new Vector3(500, -3000, 0), 40f, path), "The place is far from the ground.");
        Assert.IsFalse(field.TryFindPath(new Vector3(500, -3000, 0), new Vector3(500, 500, 0), 40f, path));
    }

    [TestMethod]
    public void AClosedGateClosesTheWayUntilItIsOpened()
    {
        var field = new NavField(Corner());
        var gate = field.AddBlocker(new Vector3(1500, 500, 0), new Vector3(100, 1000, 300), 0f, enabled: true);
        var path = new List<Vector3>();

        Assert.IsFalse(field.TryFindPath(new Vector3(500, 500, 0), new Vector3(2500, 500, 0), 40f, path));

        var end = field.Clamp(new Vector3(500, 500, 0), new Vector3(2500, 500, 0), 40f, out var reached);

        Assert.IsFalse(reached);
        Assert.IsLessThan(1450f, end.X);

        field.SetBlocker(gate, false);

        Assert.IsTrue(field.TryFindPath(new Vector3(500, 500, 0), new Vector3(2500, 500, 0), 40f, path));
    }

    [TestMethod]
    public void AGateThatIsTurnedClosesTheWayAsWell()
    {
        var field = new NavField(Squares((0, 0), (1, 0)));

        // A box 100 wide and 1000 long, turned a quarter, stands across the way.
        field.AddBlocker(new Vector3(1000, 500, 0), new Vector3(1000, 100, 300), 90f, enabled: true);

        Assert.IsFalse(field.TryFindPath(new Vector3(500, 500, 0), new Vector3(1500, 500, 0), 40f, []));
    }

    [TestMethod]
    public void ACreatureNeedsGroundAroundIt()
    {
        var field = new NavField(Squares((0, 0)));

        Assert.IsTrue(field.IsWalkable(new Vector3(500, 500, 0), 100f));
        Assert.IsFalse(field.IsWalkable(new Vector3(50, 500, 0), 100f));
    }

    // ---- The files of the client ------------------------------------------------------------

    private static IEnumerable<string> Files()
    {
        if (Folder is null || !Directory.Exists(Folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_NAVMESH is not set.");
        }

        return Directory.EnumerateFiles(Folder, "*.hkt").Order();
    }

    [TestMethod]
    public void EveryFileOfTheClientIsRead()
    {
        var count = 0;

        foreach (var file in Files())
        {
            var mesh = NavMesh.Load(file);

            Assert.IsGreaterThan(0, mesh.FaceCount, file);
            Assert.IsGreaterThan(0, mesh.EdgeCount, file);

            for (var face = 0; face < mesh.FaceCount; face++)
            {
                var info = mesh.GetFace(face);
                var area = 0f;

                for (var i = 0; i < info.EdgeCount; i++)
                {
                    var edge = mesh.GetEdge(info.FirstEdge + i);
                    var next = mesh.GetEdge(info.FirstEdge + (i + 1) % info.EdgeCount);

                    Assert.AreEqual(next.From, edge.To, $"{file}: face {face} is not a ring.");

                    var from = mesh.GetVertex(edge.From);
                    var to = mesh.GetVertex(edge.To);

                    var origin = mesh.GetVertex(mesh.GetEdge(info.FirstEdge).From);

                    area += (from.X - origin.X) * (to.Y - origin.Y) - (to.X - origin.X) * (from.Y - origin.Y);

                    if (edge.OppositeEdge >= 0)
                    {
                        var opposite = mesh.GetEdge(edge.OppositeEdge);

                        Assert.AreEqual(info.FirstEdge + i, opposite.OppositeEdge, $"{file}: neighbours do not point at each other.");
                        Assert.AreEqual(edge.From, opposite.To, $"{file}: neighbours do not touch.");
                        Assert.AreEqual(edge.To, opposite.From, $"{file}: neighbours do not touch.");
                    }
                }

                Assert.IsGreaterThan(0f, area, $"{file}: face {face} does not go round counterclockwise.");
            }

            count++;
        }

        Assert.IsGreaterThanOrEqualTo(100, count);
    }

    [TestMethod]
    public void TheMeshOfTheFirstMazeHasTheShapeTheFileSays()
    {
        var mesh = NavMesh.Load(Path.Combine(Files().First(e => e.EndsWith("M04_RSQUARE_EP_01.hkt", StringComparison.Ordinal))));

        Assert.AreEqual(42, mesh.FaceCount);
        Assert.AreEqual(196, mesh.EdgeCount);
        Assert.AreEqual(118, mesh.VertexCount);

        // The floor of that maze is a hundred centimeters high and flat.
        Assert.AreEqual(100f, mesh.Minimum.Z, 1f);
        Assert.AreEqual(100f, mesh.Maximum.Z, 10f);
        Assert.IsGreaterThan(24000f, mesh.Minimum.X);
        Assert.IsLessThan(53000f, mesh.Maximum.X);
    }

    [TestMethod]
    public void AFileThatIsNotATagfileIsRefused()
    {
        Assert.Throws<InvalidDataException>(() => NavMesh.Parse(new byte[64]));
        Assert.Throws<InvalidDataException>(() => NavMesh.Parse([]));
    }

    [TestMethod]
    public void AFileThatIsCutShortIsRefusedAndNotLoopedOn()
    {
        var file = Files().First(e => e.EndsWith("M04_RSQUARE_EP_01.hkt", StringComparison.Ordinal));
        var data = File.ReadAllBytes(file);

        foreach (var length in new[] { 9, 40, 200, 700, data.Length / 2, data.Length - 1 })
        {
            Assert.Throws<InvalidDataException>(() => NavMesh.Parse(data.AsSpan(0, length)), $"cut at {length}");
        }
    }
}
