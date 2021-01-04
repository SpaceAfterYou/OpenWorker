using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.CS;
using ow.Framework.Utils;
using ow.Service.Auth.Network.Sync;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ow.Service.Auth
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Server _server;

        public Worker(Server server, ILogger<Worker> logger, IServiceProvider service)
        {
            _logger = logger;
            _server = server;

            CommonUtils.PrintEnvironment(_logger);

            /// Activate LanContext
            service.GetRequiredService<CSClient>();
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