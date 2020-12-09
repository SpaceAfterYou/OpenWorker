using LoginService.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Lan;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LoginService
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Server _server;

        public Worker(ILogger<Worker> logger, Server server, IServiceProvider service)
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