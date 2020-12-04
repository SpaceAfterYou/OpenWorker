using Core.Systems.NetSystem;
using Core.Systems.NetSystem.Providers;
using DistrictService.Systems.GameSystem;
using Microsoft.Extensions.Logging;

namespace DistrictService.Systems.NetSystem
{
    public class Session : SwSession
    {
        public Channel Channel { get; set; }
        public Character Character { get; init; }

        public Session(Server server, HandlerProvider provider, ILogger logger) : base(server, provider, logger)
        {
        }
    }
}