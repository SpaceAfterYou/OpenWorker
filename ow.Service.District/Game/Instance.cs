using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Datas.World.Table;
using ow.Framework.IO.File.World;

namespace ow.Service.District.Game
{
    public sealed record Instance
    {
        internal DistrictTableEntity District { get; }
        internal VRoot Place { get; }

        public Instance(IConfiguration configuration, BinTables binTable, WorldTable worldTable)
        {
            ushort id = ushort.Parse(configuration["Zone:Id"]);
            District = binTable.DistrictTable[id];
            Place = worldTable.ReadBatch(District.Batch);
        }
    }
}
