using Microsoft.Extensions.Logging;
using SoulCore.IO.Network.Sync;
using SoulCore.IO.Network.Sync.Providers;
using ow.Service.World.Game.Gate;

namespace ow.Service.World.Network.Gate.Sync
{
    public sealed class SyncSession : SSessionBase
    {
        internal Account Account { get; set; } = default!;
        internal Characters Characters { get; set; } = default!;
        internal uint Background { get; set; }

        public SyncSession(SyncServer server, HandlerProvider provider, ILogger<SyncSession> logger) : base(server, provider, logger)
        {
        }
    }
}
