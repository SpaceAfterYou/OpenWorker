﻿using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Providers;
using ow.Service.District.Game;
using System.Collections.Generic;

namespace ow.Service.District.Network
{
    internal sealed class Session : GameSession
    {
        internal Account Account { get; set; } = default!;
        internal Character Character { get; set; } = default!;
        internal Profile Profile { get; set; } = default!;
        internal DimensionMember Dimension { get; set; } = default!;
        internal Storages Storages { get; set; } = default!;
        internal SpecialOptions SpecialOptions { get; set; } = default!;
        internal IReadOnlyList<uint> Gestures { get; set; } = default!;
        public Stats Stats { get; internal set; } = default!;

        internal Session(Server server, HandlerProvider provider, ILogger<Session> logger) : base(server, provider, logger)
        {
        }
    }
}