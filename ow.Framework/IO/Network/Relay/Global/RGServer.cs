using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using System;

namespace ow.Framework.IO.Network.Relay.Global
{
    public class RGServer : RServerBase<WorldHandlerAttribute>
    {
        public RGServer(IConfiguration configuration, IServiceProvider services, ILogger logger) :
            this(configuration.GetSection("World:GlobalRelay:Host"), services, logger)
        {
        }

        protected RGServer(IConfigurationSection configuration, IServiceProvider services, ILogger logger) :
            base(configuration, services, logger)
        {
        }
    }
}