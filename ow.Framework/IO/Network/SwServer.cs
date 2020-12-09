using Microsoft.Extensions.Configuration;
using NetCoreServer;
using System.Net;

namespace ow.Framework.IO.Network
{
    public class SwServer : TcpServer
    {
        public SwServer(IConfiguration configuration) : base(IPAddress.Parse(configuration["Host:Ip"]), int.Parse(configuration["Host:Port"]))
        {
        }
    }
}
