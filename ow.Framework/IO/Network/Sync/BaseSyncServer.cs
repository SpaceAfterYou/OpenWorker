using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCoreServer;
using System;
using System.Net;

namespace ow.Framework.IO.Network.Sync
{
    public abstract class BaseSyncServer : TcpServer
    {
        private readonly ILogger<BaseSyncServer> _logger;

        protected readonly IServiceProvider Services;

        protected BaseSyncServer(IServiceProvider services, IConfiguration configuration) : base(IPEndPoint.Parse($"{configuration["Ip"]}:{configuration["Port"]}")) =>
            (_logger, Services) = (services.GetRequiredService<ILogger<BaseSyncServer>>(), services);

        public override bool Start()
        {
            _logger.LogDebug($"Listen: {Endpoint}");

            return base.Start();
        }
    }
}