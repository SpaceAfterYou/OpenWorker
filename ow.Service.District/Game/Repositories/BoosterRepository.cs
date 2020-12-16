using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public sealed class BoosterRepository : List<Booster>
    {
        public BoosterRepository(IBinTables tables, IConfiguration configuration) : base(GetItems(tables, configuration))
        {
        }

        private static IEnumerable<Booster> GetItems(IBinTables tables, IConfiguration configuration) => configuration
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