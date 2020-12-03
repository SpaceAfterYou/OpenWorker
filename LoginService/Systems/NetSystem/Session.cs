﻿using Core.Systems.NetSystem;
using Core.Systems.NetSystem.Providers;
using LoginService.Systems.GameSystem;
using Microsoft.Extensions.Logging;

namespace LoginService.Systems.NetSystem
{
    public sealed class Session : SwSession
    {
        public Account Account { get; set; }

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) :
            base(server, provider, logger)
        {
        }
    }
}