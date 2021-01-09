using Microsoft.Extensions.Configuration;
using ow.Framework;

namespace ow.Service.District.Game
{
    public sealed record GateInstance
    {
        internal string Ip { get; }
        internal ushort Port { get; }

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