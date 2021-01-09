using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.Utils;
using ow.Service.District.Network.Sync;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.District
{
    internal class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SyncServer _server;

        public Worker(SyncServer server, ILogger<Worker> logger)
        {
            _logger = logger;
            _server = server;

            CommonUtils.PrintEnvironment(_logger);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Stop();
            return Task.CompletedTask;
        }
    }
}