using Microsoft.Extensions.Configuration;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Datas.World.Table;

namespace ow.Service.District.Game
{
    public sealed record Instance
    {
        internal DistrictTableEntity District { get; }
        internal VRoot Place { get; }

        public Instance(IConfiguration configuration, BinTables binTable, WorldTables worldTable)
        {
            ushort id = ushort.Parse(configuration["Zone:Id"]);
            District = binTable.DistrictTable[id];
            Place = worldTable.Read(District.Batch);
        }
    }
}