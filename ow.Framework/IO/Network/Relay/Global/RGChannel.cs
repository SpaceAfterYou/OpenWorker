using Microsoft.Extensions.Configuration;

namespace ow.Framework.IO.Network.Relay.Global
{
    public sealed class RGChannel : RChannel
    {
        public RGChannel(IConfiguration configuration) : base(configuration.GetSection("World:Relay:Global"))
        {
        }
    }
}