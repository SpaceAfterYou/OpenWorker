using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Sync.Providers;
using ow.Service.District.Game;
using System.Collections.Generic;

namespace ow.Service.District.Network.Sync
{
    public sealed class Session : SyncSession
    {
        internal Account Account { get; set; } = default!;
        internal Character Character { get; set; } = default!;
        internal Profile Profile { get; set; } = default!;
        internal DimensionMember Dimension { get; set; } = default!;
        internal Storages Storages { get; set; } = default!;
        internal SpecialOptions SpecialOptions { get; set; } = default!;
        internal IReadOnlyList<uint> Gestures { get; set; } = default!;
        internal Stats Stats { get; set; } = default!;

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) : base(server, provider, logger)
        {
        }
    }
}