using Microsoft.Extensions.Configuration;
using ow.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Gate.Game.Repository
{
    public sealed class DistrictRepository : Dictionary<ushort, DistrictRepository.Entity>
    {
        public sealed record Entity
        {
            internal string Ip { get; }
            internal ushort Port { get; }

            internal Entity(DistrictConfiguration configuration) => (Ip, Port) = (configuration.Host.Ip, configuration.Host.Port);
        }

        public DistrictRepository(IConfiguration configuration, GateInstance instance) : base(GetDistricts(configuration, instance))
        {
        }

        private static IEnumerable<KeyValuePair<ushort, Entity>> GetDistricts(IConfiguration configuration, GateInstance instance) => configuration
            .GetSection("Districts").Get<IReadOnlyDictionary<string, DistrictConfiguration>>()
            .Where(s => s.Value.Gate == instance.Id)
            .Select(s => KeyValuePair.Create(s.Value.Location, new Entity(s.Value)));
    }
}