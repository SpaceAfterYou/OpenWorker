using Microsoft.Extensions.Configuration;
using SoulCore.Data.Bin.Table.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public sealed class BoosterRepository : List<BoosterRepository.Entity>
    {
        public BoosterRepository(BinTable tables, IConfiguration configuration) : base(GetItems(tables, configuration))
        {
        }

        public record Entity
        {
            public readonly BoosterEntity Prototype;
            public readonly DateTimeOffset End;

            public Entity(BoosterEntity prototype, DateTimeOffset end)
            {
                Prototype = prototype;
                End = end;
            }
        }

        private static IEnumerable<Entity> GetItems(BinTable tables, IConfiguration configuration) => configuration
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