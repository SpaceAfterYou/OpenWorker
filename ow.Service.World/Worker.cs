using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.Utils;
using ow.Service.District.Network.Relay;
using ow.Service.District.Network.Sync;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.District
{
    public class Worker : IHostedService
    {
        private readonly SyncServer _syncServer;
        private readonly RWServer _relayWorldserver;

        public Worker(SyncServer server, RWServer relayWorldserver, ILogger<Worker> logger)
        {
            _syncServer = server;
            _relayWorldserver = relayWorldserver;

            CommonUtils.PrintEnvironment(logger);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!_syncServer.LogicThread.IsCompleted)
            {
                var q = await _syncServer.LogicThread.DequeueAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _syncServer.Start();
            _relayWorldserver.Start();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _relayWorldserver.ShutdownAsync();
            _syncServer.Stop();
        }
    }
}