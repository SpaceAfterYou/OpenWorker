using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network.Relay;
using static ow.Framework.IO.Network.Relay.World.Client.Protos.RWCPartyProto;
using static ow.Framework.IO.Network.Relay.World.Client.Protos.RWCSessionProto;

namespace ow.Core.World.Network.Relay
{
    public sealed class RWClient
    {
        public readonly RWCPartyProtoClient Party;
        public readonly RWCSessionProtoClient Session;

        public RWClient(IConfigurationSection configuration)
        {
            RChannel channel = new(configuration);

            Party = new(channel);
            Session = new(channel);
        }
    }
}