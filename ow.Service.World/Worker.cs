using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.Utils;
using ow.Service.World.Network.Gate.Sync;
using ow.Service.World.Network.Relay;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.World
{
    public class Worker : IHostedService
    {
        public Worker(WorldRelayServer relayServer, SyncServer server, ILogger<Worker> logger)
        {
            _logger = logger;
            _syncServer = server;
            _relayServer = relayServer;

            CommonUtils.PrintEnvironment(_logger);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _relayServer.Start();
            _syncServer.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _syncServer.Stop();
            return _relayServer.ShutdownAsync();
        }

        private readonly ILogger<Worker> _logger;
        private readonly WorldRelayServer _relayServer;
        private readonly SyncServer _syncServer;
    }
}