using Microsoft.Extensions.Configuration;
using ow.Framework;
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
            configuration.GetSection("Gates").Get<IReadOnlyList<GateConfiguration>>().Select(c => new GateInstance(c));
    }
}