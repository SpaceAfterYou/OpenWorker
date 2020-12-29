using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Datas.World.Table;
using ow.Framework.IO.File.World;

namespace ow.Service.District.Game
{
    public sealed record Instance
    {
        internal DistrictTableEntity Location { get; }
        internal VRoot Place { get; }

        public Instance(IConfiguration configuration, BinTables binTable, WorldTable worldTable)
        {
            ushort location = ushort.Parse(configuration[$"Districts:{configuration["Id"]}:Location"]);

            Location = binTable.District[location];
            Place = worldTable.ReadBatch(Location.Batch);
        }
    }
}