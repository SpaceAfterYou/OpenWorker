using Microsoft.Extensions.Configuration;
using ow.Framework.IO.File.Bin;
using SoulCore.Data.Bin.Table.Entities;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ow.Service.District.Game
{
    public sealed record BinTable
    {
        public static readonly BinTable Empty = new();

        internal readonly IReadOnlyDictionary<ushort, DistrictEntity> District;
        internal readonly IReadOnlyDictionary<ushort, MazeInfoEntity> MazeInfo;
        internal readonly IReadOnlyDictionary<ushort, BoosterEntity> Booster;
        internal readonly IReadOnlyDictionary<uint, PhotoItemEntity> PhotoItem;
        internal readonly IReadOnlyDictionary<ushort, GestureEntity> Gesture;
        internal readonly IReadOnlyDictionary<uint, ItemEntity> Item;
        internal readonly IReadOnlyDictionary<uint, PassInfoEntity> BattlePass;

        public BinTable(IConfiguration configuration)
        {
            using BinReader tables = new(configuration);

            District = tables.ReadDistrictTable();
            MazeInfo = tables.ReadMazeInfoTable();
            Booster = tables.ReadBoosterTable();
            PhotoItem = tables.ReadPhotoItemTable();
            Gesture = tables.ReadGestureTable();
            Item = tables.ReadItemTable();
            BattlePass = tables.ReadPassInfoTable();
        }

        private BinTable()
        {
            District = ImmutableDictionary<ushort, DistrictEntity>.Empty;
            MazeInfo = ImmutableDictionary<ushort, MazeInfoEntity>.Empty;
            Booster = ImmutableDictionary<ushort, BoosterEntity>.Empty;
            PhotoItem = ImmutableDictionary<uint, PhotoItemEntity>.Empty;
            Gesture = ImmutableDictionary<ushort, GestureEntity>.Empty;
            Item = ImmutableDictionary<uint, ItemEntity>.Empty;
            BattlePass = ImmutableDictionary<uint, PassInfoEntity>.Empty;
        }
    }
}