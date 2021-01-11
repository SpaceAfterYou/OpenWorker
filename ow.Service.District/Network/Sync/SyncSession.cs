using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Sync;
using ow.Framework.IO.Network.Sync.Providers;
using ow.Service.District.Game;
using System.Collections.Generic;

namespace ow.Service.District.Network.Sync
{
    public sealed class SyncSession : SSessionBase
    {
        public Account Account { get; set; } = default!;
        public Character Character { get; set; } = default!;
        public Profile Profile { get; set; } = default!;
        public ChannelMember? Channel { get; set; }
        public Storages Storages { get; set; } = default!;
        public SpecialOptions SpecialOptions { get; set; } = default!;
        public IReadOnlyList<uint> Gestures { get; set; } = default!;
        public Stats Stats { get; set; } = default!;

        public SyncSession(SyncServer server, HandlerProvider provider, ILogger<SyncSession> logger) : base(server, provider, logger)
        {
        }

        protected override void OnDisconnected()
        {
            ((SyncServer)Server).Characters.Remove(Character.Id, out _);

            Channel?.Leave();

            base.OnDisconnected();
        }
    }
}