using System.Xml.Linq;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The layers of the solution point one way: a project references the projects of
/// its own layer and of the layers below it, never above; the protocol knows nothing of the database, Redis or the batch
/// files, the runtime nothing of EF Core or Redis, the game logic nothing of EF Core. The test reads the project files of
/// <c>OpenWorker.slnx</c>; a new project has to be given its layer here, and an exception has to be written down here
/// with its reason.
/// </summary>
[TestClass]
public sealed class ProjectLayersTest
{
    /// <summary>
    /// The layers from the bottom up. The names are those of the projects without <c>OpenWorker.</c>.
    /// </summary>
    private static readonly string[][] Layers =
    [
        // What has no project of the solution under it: the wire format, the source generators, the navigation meshes.
        ["Wire", "Wire.SourceGenerators", "Commands.SourceGenerators", "Havok", "Navigation"],
        ["Domain"],
        // The settings and the readers of the client tables and of the map files, which every layer above reads.
        ["Extensions", "Res", "Batch", "SourceGenerator.ResourceStructures"],
        ["Protocol"],
        ["Runtime"],
        // Persistence references Runtime for the meter of the server, on which its metrics of Postgres are recorded.
        ["Infrastructure.Redis", "Persistence"],
        ["Gameplay", "Lua", "AuthServer.Gameplay", "DistrictServer.Gameplay"],
        // GateServer.Gameplay sends a chosen character on to the world through WorldManager and LeaveManager of Channel.
        // Server.Common holds the concrete handlers of the district and the maze on the bases of Channel, and the chat
        // commands of Commands: a peer of both, kept out of Channel because the gate loads Channel too.
        ["Channel", "GateServer.Gameplay", "Commands", "Server.Common"],
        ["DependencyInjection"],
        ["AuthServer", "GateServer", "DistrictServer", "MazeServer", "RelayServer", "LoadTest"],
        ["AuthServer.Test", "Hotspot.Test", "LoadTest.Test", "Benchmarks"]
    ];

    private static readonly string[] EfCore = ["Microsoft.EntityFrameworkCore", "Npgsql", "OpenWorker.Persistence"];
    private static readonly string[] Redis = ["Redis.OM", "StackExchange.Redis", "OpenWorker.Infrastructure.Redis"];
    private static readonly string[] Batch = ["OpenWorker.Batch"];

    /// <summary>
    /// What a project must not reach, through any project it references: packages and projects, by the start of their name.
    /// </summary>
    private static readonly (string Project, string Why, string[] Forbidden)[] Rules =
    [
        ("Protocol", "the protocol knows nothing of the database, Redis or the batch files", [.. EfCore, .. Redis, .. Batch]),
        ("Runtime", "the runtime knows nothing of the database or Redis", [.. EfCore, .. Redis]),
        ("Gameplay", "the game logic reaches the database through the stores of OpenWorker.Domain", EfCore),
        ("Lua", "the game logic reaches the database through the stores of OpenWorker.Domain", EfCore),
        ("DistrictServer.Gameplay", "the game logic reaches the database through the stores of OpenWorker.Domain", EfCore)
        // AuthServer.Gameplay and GateServer.Gameplay are not held to it yet: their login, account and character handlers
        // query the database themselves. Moving those queries behind stores is for a later step.
    ];

    private static readonly Lazy<Dictionary<string, Project>> Solution = new(Read);

    [TestMethod]
    public void EveryProjectOfTheSolutionHasItsLayer()
    {
        var placed = Layers.SelectMany(e => e).ToHashSet();

        var missing = Solution.Value.Keys.Where(e => !placed.Contains(e)).Order(StringComparer.Ordinal).ToArray();
        var gone = placed.Where(e => !Solution.Value.ContainsKey(e)).Order(StringComparer.Ordinal).ToArray();

        Assert.IsEmpty(missing, "Projects with no layer: " + string.Join(", ", missing));
        Assert.IsEmpty(gone, "Layers name projects that are not in the solution: " + string.Join(", ", gone));
    }

    [TestMethod]
    public void NoProjectReferencesAHigherLayer()
    {
        var layer = Layers
            .SelectMany((names, index) => names.Select(name => (name, index)))
            .ToDictionary(e => e.name, e => e.index);

        var upward = Solution.Value.Values
            .Where(e => layer.ContainsKey(e.Name))
            .SelectMany(project => project.References
                .Where(reference => layer.TryGetValue(reference, out var other) && other > layer[project.Name])
                .Select(reference => $"{project.Name} -> {reference}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(upward, "References to a higher layer: " + string.Join(", ", upward));
    }

    [TestMethod]
    public void LowerLayersDoNotReachTheDatabaseOrRedis()
    {
        var found = new List<string>();

        foreach (var (name, why, forbidden) in Rules)
        {
            // The shortest way to each dependency is enough to find it.
            foreach (var (dependency, path) in Reach(name).DistinctBy(e => e.Dependency))
            {
                if (forbidden.Any(prefix => dependency.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    found.Add($"{name} reaches {dependency} through {path} ({why})");
                }
            }
        }

        Assert.IsEmpty(found, string.Join("; ", found));
    }

    /// <summary>
    /// Every project and package a project reaches through its project references, itself included, with the way there.
    /// </summary>
    private static IEnumerable<(string Dependency, string Path)> Reach(string start)
    {
        var projects = Solution.Value;
        var seen = new HashSet<string>();
        var queue = new Queue<(string Name, string Path)>([(start, start)]);

        while (queue.TryDequeue(out var current))
        {
            if (!seen.Add(current.Name) || !projects.TryGetValue(current.Name, out var project))
            {
                continue;
            }

            foreach (var package in project.Packages)
            {
                yield return (package, current.Path);
            }

            foreach (var reference in project.References)
            {
                yield return ("OpenWorker." + reference, current.Path);

                queue.Enqueue((reference, current.Path + " -> " + reference));
            }
        }
    }

    private static Dictionary<string, Project> Read()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Join(root.FullName, "OpenWorker.slnx")))
        {
            root = root.Parent;
        }

        Assert.IsNotNull(root, "The solution folder was not found.");

        return XDocument.Load(Path.Join(root.FullName, "OpenWorker.slnx"))
            .Descendants("Project")
            .Select(e => (string)e.Attribute("Path")!)
            .Select(path => Project.Read(Path.Join(root.FullName, path)))
            .ToDictionary(e => e.Name);
    }

    private sealed record Project(string Name, string[] References, string[] Packages)
    {
        private const string Prefix = "OpenWorker.";

        public static Project Read(string file)
        {
            var document = XDocument.Load(file);

            var references = document.Descendants("ProjectReference")
                .Select(e => Path.GetFileNameWithoutExtension(((string)e.Attribute("Include")!).Replace('\\', '/')))
                .Select(Short)
                .ToArray();

            var packages = document.Descendants("PackageReference")
                .Select(e => (string)e.Attribute("Include")!)
                .ToArray();

            return new Project(Short(Path.GetFileNameWithoutExtension(file)), references, packages);
        }

        private static string Short(string name) => name.StartsWith(Prefix, StringComparison.Ordinal) ? name[Prefix.Length..] : name;
    }
}
