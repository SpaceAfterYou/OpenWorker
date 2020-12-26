using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Providers;
using ow.Service.Auth.Game;

namespace ow.Service.Auth.Network
{
    public sealed class Session : GameSession
    {
        public Account Account { get; set; } = default!;

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) : base(server, provider, logger)
        {
        }
    }
}