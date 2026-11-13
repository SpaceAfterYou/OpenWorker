using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.AuthServer;
using OpenWorker.DistrictServer;
using OpenWorker.GateServer;
using OpenWorker.Hotspot.Handler.Extensions;
using OpenWorker.MazeServer;
using OpenWorker.RelayServer;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Every server builds the services it starts with: no cycle, no scoped service in a singleton, no service it cannot
/// make. The host of each server is built from its own builder and settings, in a folder laid out like the one it runs
/// in, and every hosted service, handler and singleton is made; nothing is started and no port is opened.<br/>
/// Needs OPENWORKER_TEST_REDIS and OPENWORKER_TEST_POSTGRES, and OPENWORKER_TEST_RES for the servers that read tables;
/// the folders of the other test data are linked when their variables are set.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ServerStartTest
{
    private const string Redis = "OPENWORKER_TEST_REDIS";
    private const string Postgres = "OPENWORKER_TEST_POSTGRES";
    private const string Res = "OPENWORKER_TEST_RES";
    private const string ServerRes = "OPENWORKER_TEST_SERVER_RES";
    private const string Batch = "OPENWORKER_TEST_BATCH";
    private const string Action = "OPENWORKER_TEST_ACTION";
    private const string Ai = "OPENWORKER_TEST_AI";
    private const string NavMesh = "OPENWORKER_TEST_NAVMESH";

    [TestMethod]
    public void AuthServerBuildsItsServices() => Build("AuthServer", AuthServerHost.CreateBuilder, []);

    [TestMethod]
    public void GateServerBuildsItsServices() => Build("GateServer", GateServerHost.CreateBuilder, [Res]);

    [TestMethod]
    public void DistrictServerBuildsItsServices() =>
        Build("DistrictServer", DistrictServerHost.CreateBuilder, [Res], ("Instance:Location", "10003"), ("Instance:PublicPort", "10011"));

    [TestMethod]
    public void MazeServerBuildsItsServices() => Build("MazeServer", MazeServerHost.CreateBuilder, [Res]);

    [TestMethod]
    public void RelayServerBuildsItsServices() =>
        Build("RelayServer", RelayServerHost.CreateBuilder, [Res], ("Credentials:DiscordToken", "test"));

    /// <param name="data">The variables of the data the server reads while it is built.</param>
    /// <param name="settings">What the stand sets for the server besides its own settings.</param>
    private static void Build(string server, Func<string[], IHostBuilder> create, string[] data, params (string Key, string Value)[] settings)
    {
        foreach (var variable in (string[])[Redis, Postgres, .. data])
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
            {
                Assert.Inconclusive($"{variable} is not set.");
            }
        }

        var folder = Directory.CreateTempSubdirectory("openworker-start-");
        var previous = Directory.GetCurrentDirectory();

        try
        {
            LayOut(folder.FullName, server);

            // The servers read their tables, scripts and settings from the folder they run in.
            Directory.SetCurrentDirectory(folder.FullName);

            BuildIn(create, settings, ServerAssemblies(create.Method.DeclaringType!.Assembly));
        }

        finally
        {
            Directory.SetCurrentDirectory(previous);
            folder.Delete(recursive: true);
        }
    }

    private static void BuildIn(Func<string[], IHostBuilder> create, (string Key, string Value)[] settings, HashSet<string> assemblies)
    {
        IServiceCollection? registered = null;

        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Redis"] = Environment.GetEnvironmentVariable(Redis),
            ["ConnectionStrings:Postgres"] = Environment.GetEnvironmentVariable(Postgres) + ";Database=openworker_server_start",
            ["Telemetry:Endpoint"] = string.Empty
        };

        foreach (var (key, value) in settings)
        {
            values[key] = value;
        }

        var builder = create([])
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(values))
            .ConfigureServices(services =>
            {
                // This process has the handlers of every server loaded; a server has only those of its own assemblies.
                foreach (var foreign in services.Where(e => e.ImplementationType is { } type && type.IsHotspotHandler() && !assemblies.Contains(type.Assembly.GetName().Name!)).ToArray())
                {
                    services.Remove(foreign);
                }

                registered = services;
            })
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            });

        using var host = builder.Build();

        _ = host.Services.GetServices<IHostedService>().ToArray();

        foreach (var type in registered!
                     .Where(e => e is { Lifetime: ServiceLifetime.Singleton, IsKeyedService: false } && !e.ServiceType.ContainsGenericParameters)
                     .Select(e => e.ServiceType)
                     .Distinct())
        {
            _ = host.Services.GetServices(type).ToArray();
        }
    }

    /// <summary>
    /// The server's assembly and those it references, by name: the only ones the server can load.
    /// </summary>
    private static HashSet<string> ServerAssemblies(Assembly server)
    {
        var names = new HashSet<string>(StringComparer.Ordinal) { server.GetName().Name! };
        var pending = new Stack<Assembly>([server]);

        while (pending.TryPop(out var assembly))
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name is { } name && name.StartsWith("OpenWorker", StringComparison.Ordinal) && names.Add(name))
                {
                    pending.Push(Assembly.Load(reference));
                }
            }
        }

        return names;
    }

    /// <summary>
    /// The folder a server runs in: its settings, and its data folders linked to the test data.
    /// </summary>
    private static void LayOut(string folder, string server)
    {
        File.Copy(Path.Join(AppContext.BaseDirectory, "ServerSettings", server + ".json"), Path.Join(folder, "appsettings.json"));

        Link(folder, "res", Environment.GetEnvironmentVariable(Res));
        Link(folder, "res-server", Environment.GetEnvironmentVariable(ServerRes));
        Link(folder, "batch", Environment.GetEnvironmentVariable(Batch));
        Link(folder, "action", Environment.GetEnvironmentVariable(Action));
        Link(folder, "scripts", Path.GetDirectoryName(Environment.GetEnvironmentVariable(Ai)?.TrimEnd('/', '\\')));
        Link(folder, "navmesh", Environment.GetEnvironmentVariable(NavMesh));
    }

    private static void Link(string folder, string name, string? target)
    {
        if (!string.IsNullOrEmpty(target))
        {
            Directory.CreateSymbolicLink(Path.Join(folder, name), target);
        }
    }
}
