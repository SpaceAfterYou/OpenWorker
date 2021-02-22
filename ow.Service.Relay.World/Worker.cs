using Microsoft.Extensions.Hosting;
using ow.Service.Relay.World.Network;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.Relay.World
{
    public class Worker : IHostedService
    {
        private readonly RWServer _server;

        public Worker(RWServer server) => _server = server;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => _server.ShutdownAsync();
    }
}