using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Providers;
using ow.Service.Login.Game;

namespace ow.Service.Login.Network
{
    public sealed class Session : GameSession
    {
        public Account Account { get; set; } = default!;

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) : base(server, provider, logger)
        {
        }
    }
}