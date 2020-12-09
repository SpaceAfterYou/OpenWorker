using Microsoft.Extensions.Configuration;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Datas.World.Table;

namespace DistrictService.Game
{
    public class Zone
    {
        public VRoot Place { get; }
        public DistrictTableEntity Table { get; }

        public Zone(IConfiguration configuration, DataBinTable binTable, WorldTable worldTableProcessor)
        {
            Table = binTable.DistrictTable[ushort.Parse(configuration["Zone:Id"])];
            Place = worldTableProcessor.Read(Table.Batch);
        }
    }
}