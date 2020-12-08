using Core.Systems.GameSystem;
using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using Core.Systems.GameSystem.Datas.World.Table;
using Microsoft.Extensions.Configuration;

namespace DistrictService.Systems.GameSystem
{
    public class Zone
    {
        public VRoot Place { get; }
        public DistrictTableEntity Table { get; }

        public Zone(IConfiguration configuration, BinTable binTable)
        {
            Table = binTable.DistrictTable[ushort.Parse(configuration["Zone:Id"])];

            WorldTableProcessor wtp = new(configuration);
            Place = wtp.Read(Table.Batch);
        }
    }
}