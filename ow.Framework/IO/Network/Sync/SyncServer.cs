using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCoreServer;
using System;
using System.Net;

namespace ow.Framework.IO.Network.Sync
{
    public abstract class SyncServer : TcpServer
    {
        private readonly ILogger<SyncServer> _logger;

        protected readonly IServiceProvider Services;

        protected SyncServer(IServiceProvider services, IConfiguration configuration) : base(IPAddress.Parse(configuration["Host:Ip"]), int.Parse(configuration["Host:Port"])) =>
            (_logger, Services) = (services.GetRequiredService<ILogger<SyncServer>>(), services);

        protected SyncServer(IServiceProvider services, string ip, ushort port) : base(ip, port) =>
            (_logger, Services) = (services.GetRequiredService<ILogger<SyncServer>>(), services);

        protected SyncServer(IServiceProvider services, IPEndPoint endPoint) : base(endPoint) =>
            (_logger, Services) = (services.GetRequiredService<ILogger<SyncServer>>(), services);

        public override bool Start()
        {
            _logger.LogDebug($"Listen {Endpoint}");

            return base.Start();
        }
    }
}