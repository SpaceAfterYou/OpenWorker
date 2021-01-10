using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network.Relay.Global.Protos;

namespace ow.Framework.IO.Network.Relay.Global
{
    public class RGClient
    {
        public RGSSessionProto.RGSSessionProtoClient Session { get; }

        public RGClient(IConfiguration configuration) =>
            Session = new(new RChannel(configuration.GetSection("World:Relay:Global:Host")));

        protected RGClient(IConfigurationSection configuration) =>
            Session = new(new RChannel(configuration));
    }
}