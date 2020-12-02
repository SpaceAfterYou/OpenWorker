using LoginService.Systems.NetSystem;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace LoginService
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Server _server;

        public Worker(ILogger<Worker> logger, Server server)
        {
            _logger = logger;
            _server = server;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var q = _server.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Stop();
            return Task.CompletedTask;
        }
    }
}