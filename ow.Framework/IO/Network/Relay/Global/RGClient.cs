using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network.Relay.Global.Protos;
using ow.Framework.IO.Network.Relay.World;

namespace ow.Framework.IO.Network.Relay.Global
{
    public class RGClient
    {
        public RGSSessionProto.RGSSessionProtoClient Session { get; }

        public RGClient(IConfiguration configuration) =>
            Session = new(new RWChannel(configuration.GetSection("World:GlobalRelay:Host")));

        protected RGClient(IConfigurationSection configuration) =>
            Session = new(new RWChannel(configuration));
    }
}