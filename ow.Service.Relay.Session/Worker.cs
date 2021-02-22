using Microsoft.Extensions.Hosting;
using ow.Framework.IO.Network.Relay.Global;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.Relay.Session
{
    public class Worker : IHostedService
    {
        private readonly RGServer _server;

        public Worker(RGServer server) => _server = server;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => _server.ShutdownAsync();
    }
}