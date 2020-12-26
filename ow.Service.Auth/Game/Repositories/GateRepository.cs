using Microsoft.Extensions.Configuration;
using ow.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Auth.Game.Repositories
{
    public sealed class GateRepository : List<GateInstance>
    {
        public GateRepository(IConfiguration configuration) : base(GetGates(configuration))
        {
        }

        public static IEnumerable<GateInstance> GetGates(IConfiguration configuration) =>
            configuration.GetSection("Gates").Get<IReadOnlyDictionary<string, GateConfiguration>>().Select(c => new GateInstance(ushort.Parse(c.Key), c.Value));
    }
}