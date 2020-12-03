using Core.Systems.NetSystem;
using Core.Systems.NetSystem.Providers;
using GateService.Systems.GameSystem;
using Microsoft.Extensions.Logging;

namespace GateService.Systems.NetSystem
{
    public sealed class Session : SwSession
    {
        public Account Account { get; set; }
        public Characters Characters { get; set; }

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) :
            base(server, provider, logger)
        {
        }
    }
}