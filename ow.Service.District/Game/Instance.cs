using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Datas.World.Table;
using ow.Framework.IO.File.World;

namespace ow.Service.District.Game
{
    internal sealed record Instance
    {
        internal DistrictTableEntity Location { get; }
        internal VRoot Place { get; }

        internal Instance(IConfiguration configuration, BinTables binTable, WorldTable worldTable)
        {
            ushort id = ushort.Parse(configuration["Zone:Id"]);

            Location = binTable.District[id];
            Place = worldTable.ReadBatch(Location.Batch);
        }
    }
}