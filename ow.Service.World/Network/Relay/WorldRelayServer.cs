using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.World;
using System;

namespace ow.Service.World.Network.Relay
{
    public sealed class WorldRelayServer : RWBase
    {
        public WorldRelayServer(IConfiguration configuration, IServiceProvider services, ILogger<WorldRelayServer> logger) :
            base(configuration.GetSection($"World:Instance:{configuration["World"]}:Relay:Host"), services, logger)
        {
        }
    }
}