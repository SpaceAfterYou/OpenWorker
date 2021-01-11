using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.World;
using System;

namespace ow.Service.World.Network.Relay
{
    public sealed class RWServer : RWSBase
    {
        public RWServer(IConfiguration configuration, IServiceProvider services, ILogger<RWServer> logger) :
            base(configuration.GetSection($"World:Instance:{configuration["World"]}:Relay:World:Host"), services, logger)
        {
        }
    }
}