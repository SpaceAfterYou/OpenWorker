using Microsoft.Extensions.Configuration;
using ow.Framework.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Login.Game.Gate.Repository
{
    public sealed partial class DistrictRepository : Dictionary<ushort, DistrictRepository.DistrictEntity>
    {
        public DistrictRepository(IConfiguration configuration) : base(GetEntities(configuration))
        {
        }

        private static IEnumerable<KeyValuePair<ushort, DistrictEntity>> GetEntities(IConfiguration configuration) => configuration
            .GetSection($"World:Instance:{configuration["World"]}").Get<InstanceConfiguration>().District
            .Select(s => KeyValuePair.Create(s.Value.Location, new DistrictEntity(s.Key, s.Value, configuration)));
    }
}

// https://youtu.be/8n11Rv6RCeM