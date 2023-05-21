using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Server.Services;

internal sealed class ServerHostedService : IHostedService
{
    private readonly HotSpotServer _server;
    private readonly ILogger<ServerHostedService> _logger;

    public Task StartAsync(CancellationToken ct)
    {
        Trace.Assert(_server.Start(), "Unable to start server");

        _logger.LogInformation("Hotspot listen at {}", _server.Endpoint);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        if (_server.IsStarted)
        {
            Trace.Assert(_server.Stop(), "Unable to stop server");
        }

        return Task.CompletedTask;
    }

    public ServerHostedService(HotSpotServer server, ILogger<ServerHostedService> logger)
    {
        _server = server;
        _logger = logger;
    }
}
