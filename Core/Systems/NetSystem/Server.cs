using Microsoft.Extensions.Configuration;
using NetCoreServer;
using System.Net;

namespace Core.Systems.NetSystem
{
    public class Server : TcpServer
    {
        public Server(IConfiguration configuration) : base(IPAddress.Parse(configuration["Host:Ip"]), int.Parse(configuration["Host:Port"]))
        {
        }
    }
}