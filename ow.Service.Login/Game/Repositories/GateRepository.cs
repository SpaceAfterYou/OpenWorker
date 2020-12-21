using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Login.Game.Repositories
{
    public sealed class GateRepository : List<GateInstance>
    {
        public GateRepository(IConfiguration configuration) : base(GetGates(configuration))
        {
        }

        public static IEnumerable<GateInstance> GetGates(IConfiguration configuration) =>
            configuration.GetSection("Gates").GetChildren().AsEnumerable().Select(c => new GateInstance(c));
    }
}