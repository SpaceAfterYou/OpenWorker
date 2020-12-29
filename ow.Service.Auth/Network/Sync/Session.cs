using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Sync.Providers;
using ow.Service.Auth.Game;

namespace ow.Service.Auth.Network.Sync
{
    public sealed class Session : SyncSession
    {
        public Account Account { get; set; } = default!;

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) : base(server, provider, logger)
        {
        }
    }
}