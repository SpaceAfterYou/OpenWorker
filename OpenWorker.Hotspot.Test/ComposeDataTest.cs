using System.Text.RegularExpressions;
using System.Xml.Linq;
using OpenWorker.AuthServer;
using OpenWorker.DistrictServer;
using OpenWorker.Extensions.Data;
using OpenWorker.GateServer;
using OpenWorker.MazeServer;
using OpenWorker.RelayServer;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Every server of <c>compose.yaml</c> gets each folder of game data its kind reads, at the path it reads it from, and
/// every folder a server reads is one of <see cref="DataFolders"/>, named by the server that reads it.
/// </summary>
[TestClass]
public sealed partial class ComposeDataTest
{
    /// <summary>The folder the servers run in inside their containers.</summary>
    private const string ContainerRoot = "/app";

    private static readonly Dictionary<string, IReadOnlyList<DataNeed>> Servers = new()
    {
        ["OpenWorker.AuthServer"] = AuthServerHost.Data,
        ["OpenWorker.GateServer"] = GateServerHost.Data,
        ["OpenWorker.DistrictServer"] = DistrictServerHost.Data,
        ["OpenWorker.MazeServer"] = MazeServerHost.Data,
        ["OpenWorker.RelayServer"] = RelayServerHost.Data
    };

    [TestMethod]
    public void EveryServerOfTheComposeFileMountsTheDataItsKindReads()
    {
        var compose = Path.Join(Root(), "compose.yaml");

        if (!File.Exists(compose))
        {
            Assert.Inconclusive($"{compose} is not there.");
        }

        var services = ReadServices(File.ReadAllLines(compose));
        var missing = new List<string>();

        foreach (var (server, needs) in Servers)
        {
            var mine = services.Where(e => e.Value.Server == server).ToArray();

            if (mine.Length == 0)
            {
                missing.Add($"{server}: no service");
            }

            foreach (var (name, service) in mine)
            {
                missing.AddRange(needs
                    .Select(e => ContainerRoot + "/" + e.Folder.Path)
                    .Where(e => !service.Targets.Contains(e))
                    .Select(e => $"{name}: {e}"));
            }
        }

        Assert.IsEmpty(missing, string.Join("; ", missing));
    }

    [TestMethod]
    public void EveryFolderOfGameDataIsNamedByAServerThatReadsIt()
    {
        var named = Servers.Values.SelectMany(e => e).Select(e => e.Folder).ToHashSet();

        Assert.IsEmpty(DataFolders.All.Where(e => !named.Contains(e)).Select(e => e.Path));

        // A reader that names its folder in a literal is outside the list: the servers' code takes every path from it.
        var literals = SourceFiles()
            .SelectMany(file => File.ReadLines(file).Select((line, at) => (file, line, at)))
            .Where(e => LiteralPath().IsMatch(e.line))
            .Select(e => $"{Path.GetFileName(e.file)}:{e.at + 1}")
            .ToArray();

        Assert.IsEmpty(literals, string.Join(", ", literals));
    }

    /// <summary>
    /// The services of the compose file: the server each is built from and the container paths of its volumes.
    /// </summary>
    private static Dictionary<string, (string? Server, HashSet<string> Targets)> ReadServices(string[] lines)
    {
        var services = new Dictionary<string, (string? Server, HashSet<string> Targets)>();
        string? current = null;
        var inServices = false;
        var inVolumes = false;

        foreach (var line in lines)
        {
            if (line.Length == 0 || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (!char.IsWhiteSpace(line[0]))
            {
                inServices = line.StartsWith("services:", StringComparison.Ordinal);
                current = null;
                continue;
            }

            if (!inServices)
            {
                continue;
            }

            if (ServiceName().Match(line) is { Success: true } service)
            {
                current = service.Groups[1].Value;
                services[current] = (null, []);
                inVolumes = false;
                continue;
            }

            if (current is null)
            {
                continue;
            }

            if (Dockerfile().Match(line) is { Success: true } dockerfile)
            {
                services[current] = (dockerfile.Groups[1].Value, services[current].Targets);
            }

            if (Key().Match(line) is { Success: true } key)
            {
                inVolumes = key.Groups[1].Value == "volumes";
                continue;
            }

            if (inVolumes && Volume().Match(line) is { Success: true } volume)
            {
                services[current].Targets.Add(volume.Groups[1].Value);
            }
        }

        return services;
    }

    private static IEnumerable<string> SourceFiles()
    {
        var root = Root();

        return XDocument.Load(Path.Join(root, "OpenWorker.slnx"))
            .Descendants("Project")
            .Select(e => Path.GetDirectoryName(e.Attribute("Path")!.Value)!)
            .Where(e => !e.EndsWith(".Test", StringComparison.Ordinal) && !e.Contains("SourceGenerator", StringComparison.Ordinal)
                && e is not "OpenWorker.Benchmarks" and not "OpenWorker.LoadTest")
            .SelectMany(e => Directory.EnumerateFiles(Path.Join(root, e), "*.cs", SearchOption.AllDirectories))
            .Where(e => !e.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !e.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static string Root()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Join(root.FullName, "OpenWorker.slnx")))
        {
            root = root.Parent;
        }

        Assert.IsNotNull(root, "The checkout of the test is not found.");

        return root.FullName;
    }

    [GeneratedRegex(@"^  ([\w.-]+):\s*$")]
    private static partial Regex ServiceName();

    [GeneratedRegex(@"^\s+dockerfile:\s*""?([\w.-]+)/Dockerfile""?\s*$")]
    private static partial Regex Dockerfile();

    [GeneratedRegex(@"^    ([\w.-]+):")]
    private static partial Regex Key();

    /// <summary>The container path of a volume, after the last colon that starts one: sources hold colons of their own.</summary>
    [GeneratedRegex(@"^\s+-\s+""?.*:(/[^:\s""]+)(?::(?:ro|rw))?""?\s*$")]
    private static partial Regex Volume();

    [GeneratedRegex(@"\b(Path\.(Join|Combine)|File\.\w+|Directory\.\w+)\(\s*""")]
    private static partial Regex LiteralPath();
}
