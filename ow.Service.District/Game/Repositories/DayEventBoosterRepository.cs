using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public class DayEventBoosterRepository : List<DayEventBooster>
    {
        public DayEventBoosterRepository(Instance zone, BinTables tables) : base(GetItems(zone, tables))
        { }

        private static IEnumerable<DayEventBooster> GetItems(Instance zone, BinTables binTables) =>
            binTables.MazeInfoTable.Values.Where(c => c.District == zone.District.Id).Select(c => new DayEventBooster(0, c));
    }
}