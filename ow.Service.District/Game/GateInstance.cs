using Microsoft.Extensions.Configuration;

namespace ow.Service.District.Game
{
    internal sealed record GateInstance
    {
        internal string Ip { get; }
        internal ushort Port { get; }

        internal GateInstance(IConfiguration configuration) => (Ip, Port) = (configuration["Gate:Host:Ip"], ushort.Parse(configuration["Gate:Host:Port"]));
    }
}