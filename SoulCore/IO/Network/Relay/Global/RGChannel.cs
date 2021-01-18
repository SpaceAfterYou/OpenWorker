using Microsoft.Extensions.Configuration;

namespace SoulCore.IO.Network.Relay.Global
{
    public sealed class RGChannel : RChannel
    {
        public RGChannel(IConfiguration configuration) : base(configuration.GetSection("World:Relay:Global"))
        {
        }
    }
}
