using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    internal sealed class BoosterRepository : List<BoosterRepository.Entity>
    {
        public BoosterRepository(BinTables tables, IConfiguration configuration) : base(GetItems(tables, configuration))
        {
        }

        internal record Entity
        {
            internal BoosterTableEntity Prototype { get; }
            internal DateTimeOffset End { get; }

            internal Entity(BoosterTableEntity prototype, DateTimeOffset end) => (Prototype, End) = (prototype, end);
        }

        private static IEnumerable<Entity> GetItems(BinTables tables, IConfiguration configuration) => configuration
            .GetSection("Boosters")
            .Get<IList<IConfigurationBooster>>()
            .Select(c => new Entity(tables.Booster[c.Id], new(DateTime.Now.AddSeconds(c.Duration))));

        private interface IConfigurationBooster
        {
            ushort Id { get; }
            ulong Duration { get; }
        }
    }
}