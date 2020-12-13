using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.Gate
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly GameServer _server;

        public Worker(GameServer server, ILogger<Worker> logger, IServiceProvider service)
        {
            _logger = logger;
            _server = server;

            /// Activate LanContext
            service.GetRequiredService<LanContext>();
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