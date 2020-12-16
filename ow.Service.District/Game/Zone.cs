using Microsoft.Extensions.Configuration;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Datas.World.Table;

namespace ow.Service.District.Game
{
    public sealed record Zone
    {
        internal ushort Id { get; }
        internal DistrictTableEntity Table { get; }
        internal VRoot Place { get; }

        public Zone(IConfiguration configuration, BinTables binTable, WorldTables worldTable)
        {
            Id = ushort.Parse(configuration["Zone:Id"]);
            Table = binTable.DistrictTable[Id];
            Place = worldTable.Read(Table.Batch);
        }
    }
}