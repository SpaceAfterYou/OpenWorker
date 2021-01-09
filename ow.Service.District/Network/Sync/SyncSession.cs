using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Sync;
using ow.Framework.IO.Network.Sync.Providers;
using ow.Service.District.Game;
using System.Collections.Generic;

namespace ow.Service.District.Network.Sync
{
    public sealed class SyncSession : BaseSyncSession
    {
        internal Account Account { get; set; } = default!;
        internal Character Character { get; set; } = default!;
        internal Profile Profile { get; set; } = default!;
        internal ChannelMember? Channel { get; set; } = default!;
        internal Storages Storages { get; set; } = default!;
        internal SpecialOptions SpecialOptions { get; set; } = default!;
        internal IReadOnlyList<uint> Gestures { get; set; } = default!;
        internal Stats Stats { get; set; } = default!;

        public SyncSession(SyncServer server, HandlerProvider provider, ILogger<SyncSession> logger) : base(server, provider, logger)
        {
        }

        protected override void OnDisconnected()
        {
            Channel?.Leave();

            base.OnDisconnected();
        }
    }
}