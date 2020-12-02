using Microsoft.Extensions.Configuration;
using NetCoreServer;
using System.Net;

namespace Core.Systems.NetSystem
{
    public class SwServer : TcpServer
    {
        public SwServer(IConfiguration configuration) : base(IPAddress.Parse(configuration["Host:Ip"]), int.Parse(configuration["Host:Port"]))
        {
        }
    }
}