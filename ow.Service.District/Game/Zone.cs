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

        public Zone(IConfiguration configuration, BinTable binTable, WorldTable worldTableProcessor)
        {
            Table = binTable.DistrictTable[ushort.Parse(configuration["Zone:Id"])];
            Place = worldTableProcessor.Read(Table.Batch);
        }
    }
}