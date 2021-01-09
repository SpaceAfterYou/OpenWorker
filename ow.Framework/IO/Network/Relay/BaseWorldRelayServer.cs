using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using System;

namespace ow.Framework.IO.Network.Relay
{
    public abstract class BaseWorldRelayServer : BaseRelayServer<WorldHandlerAttribute>
    {
        protected BaseWorldRelayServer(IConfigurationSection configuration, IServiceProvider services, ILogger logger) :
            base(configuration, services, logger)
        {
        }
    }
}