using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Sync;
using ow.Framework.IO.Network.Sync.Providers;
using ow.Service.World.Game.Gate;

namespace ow.Service.World.Network.Gate.Sync
{
    public sealed class SyncSession : BaseSyncSession
    {
        internal Account Account { get; set; } = default!;
        internal Characters Characters { get; set; } = default!;
        internal uint Background { get; set; }

        public SyncSession(SyncServer server, HandlerProvider provider, ILogger<SyncSession> logger) : base(server, provider, logger)
        {
        }
    }
}