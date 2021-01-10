using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network.Relay.World;

namespace ow.Framework.IO.Network.Relay.Global
{
    public sealed class RGChannel : RWChannel
    {
        public RGChannel(IConfiguration configuration) : base(configuration.GetSection("World:GlobalRelay"))
        {
        }
    }
}
