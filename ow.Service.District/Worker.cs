using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.District
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly GameServer _server;

        public Worker(GameServer server, ILogger<Worker> logger)
        {
            _logger = logger;
            _server = server;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();
            _logger.LogDebug($"Listen {_server.Endpoint}");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Stop();
            return Task.CompletedTask;
        }
    }
}