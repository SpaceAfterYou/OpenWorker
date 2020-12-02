using Core.Systems.NetSystem;
using Core.Systems.NetSystem.Providers;
using Microsoft.Extensions.Logging;

namespace LoginService.Systems.NetSystem
{
    public class Session : SwSession
    {
        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) : base(server, provider, logger)
        {
        }
    }
}