using ow.Framework.Game.Datas.Bin.Table;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    public class MazeDayEventBoosterRepository : List<MazeDayEventBooster>
    {
        public MazeDayEventBoosterRepository(Instance zone, IBinTables binTables) : base(GetItems(zone, binTables))
        { }

        private static IEnumerable<MazeDayEventBooster> GetItems(Instance zone, IBinTables binTables) =>
            binTables.MazeInfoTable.Values.Where(c => c.District == zone.District.Id).Select(c => new MazeDayEventBooster(0, c));
    }
}