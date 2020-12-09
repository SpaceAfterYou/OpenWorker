using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network;

namespace ow.Service.District.Network
{
    public class Server : GameServer
    {
        public ushort ZoneId { get; init; }

        public Server(IConfiguration configuration) : base(configuration)
        {
            ZoneId = ushort.Parse("ZoneId");
        }
    }
}
