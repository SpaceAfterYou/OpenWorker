using Microsoft.Extensions.Configuration;

namespace ow.Framework.IO.Network.Relay
{
    public sealed class GlobalRelayChannel : WorldRelayChannel
    {
        public GlobalRelayChannel(IConfiguration configuration) : base(configuration.GetSection("World:Relay"))
        {
        }
    }
}