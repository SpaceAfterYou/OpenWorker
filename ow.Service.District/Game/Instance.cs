using Microsoft.Extensions.Configuration;
using SoulCore.Game.Datas.Bin.Table.Entities;
using SoulCore.Game.Datas.World.Table;
using SoulCore.IO.File.World;
using ow.Service.District.Network.Sync;
using System.Collections.Concurrent;

namespace ow.Service.District.Game
{
    public sealed record Instance
    {
        public DistrictTableEntity Location { get; }
        public VRoot Place { get; }
        public ConcurrentDictionary<int, SyncSession> Players { get; } = new();

        public Instance(IConfiguration configuration, BinTables binTable, WorldTable worldTable)
        {
            ushort location = ushort.Parse(configuration[$"World:Instance:{configuration["World"]}:District:{configuration["District"]}:Location"]);

            Location = binTable.District[location];
            Place = worldTable.ReadBatch(Location.Batch);
        }
    }
}
