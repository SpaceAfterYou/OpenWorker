using Microsoft.Extensions.Configuration;
using ow.Framework;
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

            internal Entity(GateConfiguration.DistrictConfiguration configuration) => (Ip, Port) = (configuration.Host.Ip, configuration.Host.Port);
        }

        public DistrictRepository(IConfiguration configuration) : base(GetDistricts(configuration))
        {
        }

        private static IEnumerable<KeyValuePair<ushort, Entity>> GetDistricts(IConfiguration configuration)
        {
            ushort id = ushort.Parse(configuration["Id"]);

            return configuration
                .GetSection("Gates").Get<IReadOnlyList<GateConfiguration>>()
                .First(c => c.Id == id).Districts
                .Select(s => KeyValuePair.Create(s.Location, new Entity(s)));
        }
    }
}