using Core.Systems.NetSystem;
using Microsoft.Extensions.Configuration;

namespace DistrictService.Systems
{
    public class Server : SwServer
    {
        public Server(IConfiguration configuration) : base(configuration)
        {
        }
    }
}