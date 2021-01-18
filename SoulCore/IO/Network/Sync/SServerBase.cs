using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCoreServer;
using System;
using System.Net;

namespace SoulCore.IO.Network.Sync
{
    public abstract class SServerBase : TcpServer
    {
        private readonly ILogger<SServerBase> _logger;

        protected readonly IServiceProvider Services;

        protected SServerBase(IServiceProvider services, IConfiguration configuration) : base(IPEndPoint.Parse($"{configuration["Ip"]}:{configuration["Port"]}")) =>
            (_logger, Services) = (services.GetRequiredService<ILogger<SServerBase>>(), services);

        public override bool Start()
        {
            _logger.LogDebug($"Listen: {Endpoint}");

            return base.Start();
        }
    }
}
