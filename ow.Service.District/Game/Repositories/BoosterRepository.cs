using Microsoft.Extensions.Configuration;
using SoulCore.Game.Datas.Bin.Table.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public sealed class BoosterRepository : List<BoosterRepository.Entity>
    {
        public BoosterRepository(BinTables tables, IConfiguration configuration) : base(GetItems(tables, configuration))
        {
        }

        public record Entity
        {
            public BoosterTableEntity Prototype { get; }
            public DateTimeOffset End { get; }

            public Entity(BoosterTableEntity prototype, DateTimeOffset end) => (Prototype, End) = (prototype, end);
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
