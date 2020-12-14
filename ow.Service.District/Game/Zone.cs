using Microsoft.Extensions.Configuration;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Datas.World.Table;

namespace ow.Service.District.Game
{
    public class Zone
    {
        public DistrictTableEntity Table { get; }
        public VRoot Place { get; }

        public Zone(IConfiguration configuration, BinTables binTable, WorldTables worldTable)
        {
            Table = binTable.DistrictTable[ushort.Parse(configuration["Zone:Id"])];
            Place = worldTable.Read(Table.Batch);
        }
    }
}