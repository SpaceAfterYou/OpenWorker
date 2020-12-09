using Microsoft.Extensions.Configuration;
using ow.Framework.IO.Network;

namespace DistrictService.Network
{
    public class Server : SwServer
    {
        public ushort ZoneId { get; init; }

        public Server(IConfiguration configuration) : base(configuration)
        {
            ZoneId = ushort.Parse("ZoneId");
        }
    }
}