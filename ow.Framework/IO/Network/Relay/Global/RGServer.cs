using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using System;

namespace ow.Framework.IO.Network.Relay.Global
{
    public class RGServer : RServerBase<GlobalHandlerAttribute>
    {
        public RGServer(IConfiguration configuration, IServiceProvider services, ILogger<RGServer> logger) :
            this(configuration.GetSection("World:Relay:Global:Host"), services, (ILogger)logger)
        {
        }

        protected RGServer(IConfigurationSection configuration, IServiceProvider services, ILogger logger) :
            base(configuration, services, logger)
        {
        }
    }
}