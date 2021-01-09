using Microsoft.Extensions.Configuration;

using static ow.Framework.IO.Network.Relay.Protos.SessionService;

namespace ow.Framework.IO.Network.Relay
{
    public sealed class GlobalRelayClient
    {
        public SessionServiceClient Session { get; }

        public GlobalRelayClient(IConfiguration configuration) =>
            Session = new(new WorldRelayChannel(GetHost(configuration)));

        private static IConfigurationSection GetHost(IConfiguration configuration) =>
            configuration.GetSection($"World:Relay:Host");
    }
}