using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.World.Table;
using ow.Framework.IO.File.World;
using ow.Service.District.Network.Sync;
using System.Collections.Concurrent;

namespace ow.Service.District.Game
{
    public sealed record Instance
    {
        public static readonly Instance Empty = new();

        public readonly VRoot Place;
        public readonly ushort LocationId;
        public readonly ConcurrentDictionary<int, SyncSession> Players = new();

        public Instance(IConfiguration configuration, BinTable binTable)
        {
            LocationId = ushort.Parse(configuration[$"World:Instance:{configuration["World"]}:District:{configuration["District"]}:Location"]);

            SoulCore.Data.Bin.Table.Entities.DistrictEntity? location = binTable.District[LocationId];

            using WorldTable worldTable = new(configuration);
            Place = worldTable.ReadBatch(location.Batch);
        }

        private Instance()
        {
            Place = VRoot.Empty;
        }
    }
}