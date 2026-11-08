using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;

namespace OpenWorker.DependencyInjection.Health;

public static class HealthHostingExtensions
{
    /// <summary>
    /// The process is alive and its world loop runs.
    /// </summary>
    public const string LivePath = "/healthz";

    /// <summary>
    /// The server can take players: it is alive, Redis and the database answer, the listener is up, it is not stopping.
    /// </summary>
    public const string ReadyPath = "/readyz";

    /// <summary>
    /// Serves <see cref="LivePath"/> and <see cref="ReadyPath"/> over HTTP on <c>Health:Port</c>, a port apart from
    /// the one players connect to. Kestrel is the only part of ASP.NET Core in the process; it has no routes besides
    /// these two and logs nothing for them unless <c>Microsoft.AspNetCore</c> is asked for.<br/>
    /// Call it first on the builder: the endpoint is then the first service to start (the probes can see a server that
    /// is still migrating) and the last to stop, after the drain has told them the server is going away.
    /// </summary>
    public static IHostBuilder UseHealthEndpoint(this IHostBuilder builder)
    {
        return builder
            .ConfigureServices(services => services
                .AddHealthOptions()
                .AddHealthChecks())
            .ConfigureWebHostDefaults(web => web
                .ConfigureKestrel(ListenOnHealthPort)
                .Configure(app =>
                {
                    app.UseHealthChecks(LivePath, Options(HealthTags.Live));
                    app.UseHealthChecks(ReadyPath, Options(HealthTags.Ready));
                }));
    }

    private static void ListenOnHealthPort(WebHostBuilderContext context, KestrelServerOptions kestrel)
    {
        // Read here, not before the host exists: the options are validated by now, and a wrong port has stopped the server.
        var port = kestrel.ApplicationServices.GetRequiredService<IOptions<HealthOptions>>().Value.Port;

        kestrel.ListenAnyIP(port);
    }

    private static HealthCheckOptions Options(string tag)
    {
        return new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(tag),
            ResponseWriter = WriteReportAsync
        };
    }

    /// <summary>
    /// The status code says healthy or not; the body says which check, for a person with <c>curl</c>. Exceptions are
    /// left out: the log has them.
    /// </summary>
    private static Task WriteReportAsync(HttpContext context, HealthReport report)
    {
        var checks = report.Entries.ToDictionary(
            e => e.Key,
            e => new { status = e.Value.Status.ToString(), description = e.Value.Description });

        return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString(), checks });
    }
}
