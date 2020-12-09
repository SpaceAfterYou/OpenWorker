using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Login.Game
{
    public sealed class GatesInstances : List<GateInstance>
    {
        public GatesInstances(IConfiguration configuration) : base(GetGates(configuration))
        {
        }

        public static IEnumerable<GateInstance> GetGates(IConfiguration configuration) =>
            configuration.GetSection("Gates").GetChildren().AsEnumerable().Select(c => new GateInstance(c));
    }
}
