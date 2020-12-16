using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public class MazeDayEventBoosterRepository : List<MazeDayEventBooster>
    {
        public MazeDayEventBoosterRepository(Instance zone, BinTables tables) : base(GetItems(zone, tables))
        { }

        private static IEnumerable<MazeDayEventBooster> GetItems(Instance zone, BinTables binTables) =>
            binTables.MazeInfoTable.Values.Where(c => c.District == zone.District.Id).Select(c => new MazeDayEventBooster(0, c));
    }
}