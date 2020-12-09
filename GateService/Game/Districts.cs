using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace GateService.Game
{
    public sealed class Districts : Dictionary<ushort, District>
    {
        public Districts(IConfiguration configuration) : base(GetDistricts(configuration))
        {
        }

        public static IEnumerable<KeyValuePair<ushort, District>> GetDistricts(IConfiguration configuration) =>
            configuration.GetSection("Districts").GetChildren().AsEnumerable().Select(c => KeyValuePair.Create(ushort.Parse(c["Id"]), new District(c)));
    }
}
