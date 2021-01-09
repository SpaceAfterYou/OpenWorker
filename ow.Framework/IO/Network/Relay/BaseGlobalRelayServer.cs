using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using System;

namespace ow.Framework.IO.Network.Relay
{
    public abstract class BaseGlobalRelayServer : BaseRelayServer<GlobalHandlerAttribute>
    {
        protected BaseGlobalRelayServer(IConfigurationSection configuration, IServiceProvider services, ILogger logger) :
            base(configuration, services, logger)
        {
        }
    }
}