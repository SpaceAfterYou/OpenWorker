using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace ow.Framework.IO.Network.Relay.World
{
    public class RWChannel : Channel
    {
        public RWChannel(IConfigurationSection configuration) :
            base(configuration["Ip"], int.Parse(configuration["Port"]), ChannelCredentials.Insecure)
        {
        }
    }
}