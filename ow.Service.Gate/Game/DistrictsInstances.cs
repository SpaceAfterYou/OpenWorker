using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Gate.Game
{
    public sealed class DistrictsInstances : Dictionary<ushort, DistrictInstance>
    {
        public DistrictsInstances(IConfiguration configuration) : base(GetDistricts(configuration))
        {
        }

        public static IEnumerable<KeyValuePair<ushort, DistrictInstance>> GetDistricts(IConfiguration configuration) => configuration
            .GetSection("Districts")
            .GetChildren()
            .AsEnumerable()
            .Select(c => KeyValuePair.Create(ushort.Parse(c["Id"]), new DistrictInstance(c)));
    }
}