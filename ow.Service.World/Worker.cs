using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoulCore.Utils;
using ow.Service.World.Network.Gate.Sync;
using ow.Service.World.Network.Relay;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.World
{
    public class Worker : IHostedService
    {
        private readonly SyncServer _syncServer;
        private readonly RWServer _relayServer;

        public Worker(SyncServer server, RWServer relayServer, ILogger<Worker> logger)
        {
            _relayServer = relayServer;
            _syncServer = server;

            CommonUtils.PrintEnvironment(logger);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _syncServer.Start();
            _relayServer.Start();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _relayServer.ShutdownAsync();
            _syncServer.Stop();
        }
    }
}
