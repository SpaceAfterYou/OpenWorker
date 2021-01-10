using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.World;
using System;

namespace ow.Service.World.Network.Relay
{
    public sealed class WorldRelayServer : RWSBase
    {
        public WorldRelayServer(IConfiguration configuration, IServiceProvider services, ILogger<WorldRelayServer> logger) :
            base(configuration.GetSection($"World:Instance:{configuration["World"]}:Relay:World:Host"), services, logger)
        {
        }
    }
}