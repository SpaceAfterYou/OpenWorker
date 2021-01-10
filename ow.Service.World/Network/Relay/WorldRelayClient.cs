using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network.Relay;
using static ow.Framework.IO.Network.Relay.World.Server.Protos.RWSPartyProto;

namespace ow.Service.World.Network.Relay
{
    public sealed class WorldRelayClient
    {
        public RWSPartyProtoClient Party { get; }

        public WorldRelayClient(IConfigurationSection configuration) =>
            Party = new(new RChannel(configuration));
    }
}
