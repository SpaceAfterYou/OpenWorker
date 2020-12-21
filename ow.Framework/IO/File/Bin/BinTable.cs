using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ow.Framework.IO.File.Bin
{
    public sealed class BinTable : IDisposable
    {
        private readonly BinFile _data;

        public IReadOnlyDictionary<Hero, ClassSelectInfoTableEntity> ReadClassSelectInfoTable() =>
            Read<Hero, ClassSelectInfoTableEntity>(_data, "tb_ClassSelect_Info");

        public IReadOnlyDictionary<Hero, CustomizeSkinTableEntity> ReadCustomizeSkinTable() =>
            Read<Hero, CustomizeSkinTableEntity>(_data, "tb_Customize_Skin");

        public IReadOnlyDictionary<Hero, CustomizeEyesTableEntity> ReadCustomizeEyesTable() =>
            Read<Hero, CustomizeEyesTableEntity>(_data, "tb_Customize_Eyes");

        public IReadOnlyDictionary<Hero, CustomizeHairTableEntity> ReadCustomizeHairTable() =>
            Read<Hero, CustomizeHairTableEntity>(_data, "tb_Customize_Hair");

        public IReadOnlyDictionary<Hero, CustomizeInfoTableEntity> ReadCustomizeInfoTable() =>
            Read<Hero, CustomizeInfoTableEntity>(_data, "tb_Customize_Info");

        public IReadOnlyDictionary<uint, CharacterBackgroundTableEntity> ReadCharacterBackgroundTable() =>
            Read<uint, CharacterBackgroundTableEntity>(_data, "tb_Character_Background");

        public IReadOnlyDictionary<ushort, DistrictTableEntity> ReadDistrictTable() =>
            Read<ushort, DistrictTableEntity>(_data, "tb_district");

        public IReadOnlyDictionary<uint, ItemTableEntity> ReadItemTable() =>
            Read<uint, ItemTableEntity>(_data, "tb_item");

        public IReadOnlyDictionary<uint, ItemClassifyTableEntity> ReadItemClassifyTable() =>
            Read<uint, ItemClassifyTableEntity>(_data, "tb_Item_Classify");

        public IReadOnlyDictionary<uint, ItemScriptTableEntity> ReadItemScriptTable() =>
            Read<uint, ItemScriptTableEntity>(_data, "tb_item_script");

        public IReadOnlyDictionary<ushort, CharacterInfoTableEntity> ReadCharacterInfoTable() =>
            Read<ushort, CharacterInfoTableEntity>(_data, "tb_Character_Info");

        public IReadOnlyDictionary<uint, PhotoItemTableEntity> ReadPhotoItemTable() =>
            Read<uint, PhotoItemTableEntity>(_data, "tb_Photo_Item");

        public IReadOnlyDictionary<ushort, GestureTableEntity> ReadGestureTable() =>
            Read<ushort, GestureTableEntity>(_data, "tb_Gesture");

        public IReadOnlyDictionary<ushort, MazeInfoTableEntity> ReadMazeInfoTable() =>
            Read<ushort, MazeInfoTableEntity>(_data, "tb_Maze_Info");

        public IReadOnlyDictionary<ushort, BoosterTableEntity> ReadBoosterTable() =>
            Read<ushort, BoosterTableEntity>(_data, "tb_Booster");

        public BinTable(IConfiguration configuration) => _data = new(configuration);

        internal static IReadOnlyDictionary<TId, TItem> Read<TId, TItem>(BinFile data, string file) where TId : IConvertible where TItem : ITableEntity<TId> => GetEntries(data, file)
            .Select(br => (TItem)(Activator.CreateInstance(typeof(TItem), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { br }, null) ?? throw new ApplicationException()))
            .ToDictionary(k => k.Id, v => v);

        private static IEnumerable<BinaryReader> GetEntries(BinFile data, string file)
        {
            using BinaryReader br = new(data.GetInputStream(data.GetEntry($"../bin/Table/{file}.res")));

            for (uint q = 0, count = br.ReadUInt32(); q < count; ++q)
                yield return br;
        }

        void IDisposable.Dispose()
        {
            ((IDisposable)_data).Dispose();
            GC.SuppressFinalize(this);
        }
    }
}