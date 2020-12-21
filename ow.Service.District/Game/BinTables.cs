using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.IO.File.Bin;
using System.Collections.Generic;

namespace ow.Service.District.Game
{
    internal sealed record BinTables
    {
        internal IReadOnlyDictionary<ushort, DistrictTableEntity> District { get; }
        internal IReadOnlyDictionary<ushort, MazeInfoTableEntity> MazeInfo { get; }
        internal IReadOnlyDictionary<ushort, BoosterTableEntity> Booster { get; }
        internal IReadOnlyDictionary<uint, PhotoItemTableEntity> PhotoItem { get; }
        internal IReadOnlyDictionary<ushort, GestureTableEntity> Gesture { get; }

        public BinTables(IConfiguration configuration)
        {
            using BinTable tables = new(configuration);

            District = tables.ReadDistrictTable();
            MazeInfo = tables.ReadMazeInfoTable();
            Booster = tables.ReadBoosterTable();
            PhotoItem = tables.ReadPhotoItemTable();
            Gesture = tables.ReadGestureTable();
        }
    }
}