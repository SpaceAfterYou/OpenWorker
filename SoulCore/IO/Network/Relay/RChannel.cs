using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace SoulCore.IO.Network.Relay
{
    public class RChannel : Channel
    {
        public RChannel(IConfigurationSection configuration) :
            base(configuration["Ip"], int.Parse(configuration["Port"]), ChannelCredentials.Insecure)
        {
        }
    }
}
