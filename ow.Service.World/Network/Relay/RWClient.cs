using Microsoft.Extensions.Configuration;
using SoulCore.IO.Network.Relay;
using static SoulCore.IO.Network.Relay.World.Client.Protos.RWCPartyProto;
using static SoulCore.IO.Network.Relay.World.Client.Protos.RWCSessionProto;

namespace ow.Service.World.Network.Relay
{
    public sealed class RWClient
    {
        public RWCPartyProtoClient Party { get; }
        public RWCSessionProtoClient Session { get; }

        public RWClient(IConfigurationSection configuration)
        {
            RChannel channel = new(configuration);

            Party = new(channel);
            Session = new(channel);
        }
    }
}
