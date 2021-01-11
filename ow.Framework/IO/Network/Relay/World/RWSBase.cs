using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using System;

namespace ow.Framework.IO.Network.Relay.World
{
    public abstract class RWSBase : RServerBase<WorldHandlerAttribute>
    {
        protected RWSBase(IConfigurationSection configuration, IServiceProvider services, ILogger logger) :
            base(configuration, services, logger)
        {
        }
    }
}