using Microsoft.Extensions.Configuration;

namespace ow.Service.Gate.Game
{
    public sealed record DistrictInstance
    {
        public string Ip { get; }
        public ushort Port { get; }

        public DistrictInstance(IConfigurationSection section) => (Ip, Port) = (section["Host:Ip"], ushort.Parse(section["Host:Port"]));
    }
}