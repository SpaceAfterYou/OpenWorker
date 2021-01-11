using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.IO.File.Bin;
using System.Collections.Generic;

namespace ow.Service.District.Game
{
    public sealed record BinTables
    {
        public IReadOnlyDictionary<ushort, DistrictTableEntity> District { get; }
        public IReadOnlyDictionary<ushort, MazeInfoTableEntity> MazeInfo { get; }
        public IReadOnlyDictionary<ushort, BoosterTableEntity> Booster { get; }
        public IReadOnlyDictionary<uint, PhotoItemTableEntity> PhotoItem { get; }
        public IReadOnlyDictionary<ushort, GestureTableEntity> Gesture { get; }
        public IReadOnlyDictionary<uint, ItemTableEntity> Item { get; }
        public IReadOnlyDictionary<uint, PassInfoTableEntity> BattlePass { get; }

        public BinTables(IConfiguration configuration)
        {
            using BinTable tables = new(configuration);

            District = tables.ReadDistrictTable();
            MazeInfo = tables.ReadMazeInfoTable();
            Booster = tables.ReadBoosterTable();
            PhotoItem = tables.ReadPhotoItemTable();
            Gesture = tables.ReadGestureTable();
            Item = tables.ReadItemTable();
            BattlePass = tables.ReadPassInfoTable();
        }
    }
}