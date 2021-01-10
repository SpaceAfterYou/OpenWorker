using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network.Relay;
using static ow.Framework.IO.Network.Relay.World.Server.Protos.RWSPartyProto;

namespace ow.Service.District.Network.Relay
{
    public sealed class WorldRelayClient
    {
        public RWSPartyProtoClient Party { get; }

        public WorldRelayClient(IConfiguration configuration)
        {
            Party = new(new RChannel(configuration.GetSection($"World:Instance:{configuration["World"]}:Relay:World:Host")));
        }
    }
}

// https://youtu.be/BCHRxsmWsCQ
