using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Service.World.Network.Relay;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.Relay
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly GlobalRelayServer _server;

        public Worker(GlobalRelayServer server, ILogger<Worker> logger)
        {
            _logger = logger;
            _server = server;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) =>
            _server.ShutdownAsync();
    }
}