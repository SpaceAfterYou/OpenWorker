using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public sealed class BoosterRepository : List<Booster>
    {
        public BoosterRepository(BinTables tables, IConfiguration configuration) : base(GetItems(tables, configuration))
        {
        }

        private static IEnumerable<Booster> GetItems(BinTables tables, IConfiguration configuration) => configuration
            .GetSection("Boosters")
            .Get<IList<IConfigurationBooster>>()
            .Select(c => new Booster(tables.BoosterTable[c.Id], new(DateTime.Now.AddSeconds(c.Duration))));

        private interface IConfigurationBooster
        {
            ushort Id { get; }
            ulong Duration { get; }
        }
    }
}