using SoulCore.Data.Bin.Table.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public sealed class DayEventBoosterRepository : List<DayEventBoosterRepository.DayEventBooster>
    {
        public DayEventBoosterRepository(Instance zone, BinTable tables) : base(GetItems(zone, tables))
        {
        }

        public sealed record DayEventBooster
        {
            public readonly ushort Id;
            public readonly MazeInfoEntity Maze;

            public DayEventBooster(ushort id, MazeInfoEntity maze)
            {
                Id = id;
                Maze = maze;
            }
        }

        private static IEnumerable<DayEventBooster> GetItems(Instance zone, BinTable tables) => tables.MazeInfo.Values
            .Where(s => s.District == zone.Location.Id)
            .Select(c => new DayEventBooster(0, c));
    }
}