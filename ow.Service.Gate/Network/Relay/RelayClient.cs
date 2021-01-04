using ow.Framework.IO.Network.Relay;
using ow.Framework.IO.Network.Relay.Protos;

namespace ow.Service.Gate.Network.Relay
{
    public sealed class RelayClient
    {
        public SessionRelay.SessionRelayClient Session { get; }

        public RelayClient(RelayChannel client) => Session = new(client);
    }
}