using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Gate.Game.Repository
{
    public sealed class DistrictRepository : Dictionary<ushort, DistrictInstance>
    {
        public DistrictRepository(IConfiguration configuration) : base(GetDistricts(configuration))
        {
        }

        public static IEnumerable<KeyValuePair<ushort, DistrictInstance>> GetDistricts(IConfiguration configuration) => configuration
            .GetSection("Districts")
            .GetChildren()
            .AsEnumerable()
            .Select(c => KeyValuePair.Create(ushort.Parse(c["Id"]), new DistrictInstance(c)));
    }
}