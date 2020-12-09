using Core.Systems.LanSystem;
using GateService.Systems.NetSystem;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GateService
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Server _server;

        public Worker(Server server, ILogger<Worker> logger, Runner runner)
        {
            _logger = logger;
            _server = server;

            runner.Run();
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