using Microsoft.Extensions.Configuration;
using SoulCore.IO.Network.Relay;
using static SoulCore.IO.Network.Relay.World.Server.Protos.RWSPartyProto;

namespace ow.Service.District.Network.Relay
{
    public sealed class RWClient
    {
        public RWSPartyProtoClient Party { get; }

        public RWClient(IConfiguration configuration)
        {
            Party = new(new RChannel(configuration.GetSection($"World:Instance:{configuration["World"]}:Relay:World:Host")));
        }
    }
}

// https://youtu.be/BCHRxsmWsCQ
