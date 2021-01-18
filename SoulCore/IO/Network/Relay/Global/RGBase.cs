using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SoulCore.IO.Network.Relay.Attrubutes;
using System;

namespace SoulCore.IO.Network.Relay.Global
{
    public abstract class RGBase : RServerBase<GlobalHandlerAttribute>
    {
        protected RGBase(IConfigurationSection configuration, IServiceProvider services, ILogger logger) :
            base(configuration, services, logger)
        {
        }
    }
}
