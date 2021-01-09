using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay;
using System;

namespace ow.Service.World.Network.Relay
{
    public sealed class RelayServer : BaseWorldRelayServer
    {
        public RelayServer(IConfiguration configuration, IServiceProvider services, ILogger<RelayServer> logger) :
            base(configuration.GetSection($"World:Instance:{configuration["World"]}:Relay:Host"), services, logger)
        {
        }
    }
}