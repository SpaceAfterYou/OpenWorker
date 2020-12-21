using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Providers;
using ow.Service.Gate.Game;

namespace ow.Service.Gate.Network
{
    public sealed class Session : GameSession
    {
        internal Account Account { get; set; } = default!;
        internal Characters Characters { get; set; } = default!;
        internal uint Background { get; set; }

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) : base(server, provider, logger)
        {
        }
    }
}