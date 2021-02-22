using Microsoft.Extensions.Configuration;
using ow.Framework.Configuration;

namespace ow.Service.District.Game
{
    public sealed record GateInstance
    {
        public readonly string Ip;
        public readonly ushort Port;

        public GateInstance(IConfiguration configuration)
        {
            HostConfiguration host = configuration
                .GetSection($"World:Instance:{configuration["World"]}:District:{configuration["District"]}:Host")
                .Get<HostConfiguration>();

            Ip = host.Ip;
            Port = host.Port;
        }
    }
}