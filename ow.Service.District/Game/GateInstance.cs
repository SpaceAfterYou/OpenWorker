using Microsoft.Extensions.Configuration;

namespace ow.Service.District.Game
{
    internal sealed record GateInstance
    {
        internal string Ip { get; }
        internal ushort Port { get; }

        public GateInstance(IConfiguration configuration) => (Ip, Port) = (configuration[$"Gates:{configuration["Gate"]}:Host:Ip"], ushort.Parse(configuration[$"Gates:{configuration["Gate"]}:Host:Port"]));
    }
}