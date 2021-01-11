using ow.Framework.Game.Datas.Bin.Table.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public sealed class DayEventBoosterRepository : List<DayEventBoosterRepository.Entity>
    {
        public DayEventBoosterRepository(Instance zone, BinTables tables) : base(GetItems(zone, tables))
        {
        }

        public sealed record Entity
        {
            public ushort Id { get; }
            public MazeInfoTableEntity Maze { get; }

            public Entity(ushort id, MazeInfoTableEntity maze) => (Id, Maze) = (id, maze);
        }

        private static IEnumerable<Entity> GetItems(Instance zone, BinTables tables) => tables.MazeInfo.Values
            .Where(s => s.District == zone.Location.Id)
            .Select(c => new Entity(0, c));
    }
}