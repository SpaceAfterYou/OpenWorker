using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Gate.Game.Repository
{
    internal sealed class DistrictRepository : Dictionary<ushort, DistrictRepository.Entity>
    {
        internal sealed record Entity
        {
            internal string Ip { get; }
            internal ushort Port { get; }

            internal Entity(IConfigurationSection section) => (Ip, Port) = (section["Host:Ip"], ushort.Parse(section["Host:Port"]));
        }

        public DistrictRepository(IConfiguration configuration) : base(GetDistricts(configuration))
        {
        }

        private static IEnumerable<KeyValuePair<ushort, Entity>> GetDistricts(IConfiguration configuration) => configuration
            .GetSection("Districts")
            .GetChildren()
            .AsEnumerable()
            .Select(c => KeyValuePair.Create(ushort.Parse(c["Id"]), new Entity(c)));
    }
}