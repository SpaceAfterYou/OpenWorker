using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay;
using System;

namespace ow.Service.World.Network.Relay
{
    public sealed class GlobalRelayServer : BaseGlobalRelayServer
    {
        public GlobalRelayServer(IConfiguration configuration, IServiceProvider services, ILogger<GlobalRelayServer> logger) :
            base(configuration.GetSection($"World:Relay:Host"), services, logger)
        {
        }
    }
}