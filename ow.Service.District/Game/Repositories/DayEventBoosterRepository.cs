using ow.Framework.Game.Datas.Bin.Table;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.District.Game.Repositories
{
    internal class DayEventBoosterRepository : List<DayEventBooster>
    {
        public DayEventBoosterRepository(Zone zone, IBinTables binTables) : base(GetItems(zone, binTables))
        { }

        public static IEnumerable<DayEventBooster> GetItems(Zone zone, IBinTables binTables) =>
            binTables.MazeInfoTable.Values.Where(c => c.District == zone.Id).Select(c => new DayEventBooster(0, c));
    }
}