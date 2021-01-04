using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace ow.Framework.IO.Network.Relay
{
    public class RelayChannel : Channel
    {
        public RelayChannel(IConfiguration configuration) : base(configuration["Relay:Host:Ip"], int.Parse(configuration["Relay:Host:Port"]), ChannelCredentials.Insecure)
        {
        }
    }
}