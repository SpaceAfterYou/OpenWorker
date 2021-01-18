using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SoulCore.IO.Network.Relay.World;
using System;

namespace ow.Service.District.Network.Relay
{
    public sealed class RWServer : RWSBase
    {
        public RWServer(IConfiguration configuration, IServiceProvider services, ILogger<RWServer> logger) :
            base(configuration.GetSection($"World:Instance:{configuration["World"]}:District:{configuration["District"]}:Relay:World:Host"), services, logger)
        {
        }
    }
}
