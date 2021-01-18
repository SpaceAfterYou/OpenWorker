﻿using Microsoft.Extensions.Logging;
using SoulCore.IO.Network.Sync;
using SoulCore.IO.Network.Sync.Providers;
using ow.Service.Auth.Game;

namespace ow.Service.Auth.Network.Sync
{
    public sealed class SyncSession : SSessionBase
    {
        public Account Account { get; set; } = default!;

        public SyncSession(SyncServer server, HandlerProvider provider, ILogger<SyncSession> logger) : base(server, provider, logger)
        {
        }
    }
}
