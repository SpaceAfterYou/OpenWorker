using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using ow.Framework.IO.GameFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ow.Framework.Game
{
    public class BinTable
    {
        private readonly VData12 _data;

        public IReadOnlyDictionary<Hero, IClassSelectInfoTableEntity> ReadClassSelectInfoTable() =>
            Read<Hero, ClassSelectInfoTableEntity, IClassSelectInfoTableEntity>(_data, "tb_ClassSelect_Info");

        public IReadOnlyDictionary<Hero, ICustomizeSkinTableEntity> ReadCustomizeSkinTable() =>
            Read<Hero, CustomizeSkinTableEntity, ICustomizeSkinTableEntity>(_data, "tb_Customize_Skin");

        public IReadOnlyDictionary<Hero, ICustomizeEyesTableEntity> ReadCustomizeEyesTable() =>
            Read<Hero, CustomizeEyesTableEntity, ICustomizeEyesTableEntity>(_data, "tb_Customize_Eyes");

        public IReadOnlyDictionary<Hero, ICustomizeHairTableEntity> ReadCustomizeHairTable() =>
            Read<Hero, CustomizeHairTableEntity, ICustomizeHairTableEntity>(_data, "tb_Customize_Hair");

        public IReadOnlyDictionary<Hero, ICustomizeInfoTableEntity> ReadCustomizeInfoTable() =>
            Read<Hero, CustomizeInfoTableEntity, ICustomizeInfoTableEntity>(_data, "tb_Customize_Info");

        public IReadOnlyDictionary<uint, ICharacterBackgroundTableEntity> ReadCharacterBackgroundTable() =>
            Read<uint, CharacterBackgroundTableEntity, ICharacterBackgroundTableEntity>(_data, "tb_Character_Background");

        public IReadOnlyDictionary<ushort, IDistrictTableEntity> ReadDistrictTable() =>
            Read<ushort, DistrictTableEntity, IDistrictTableEntity>(_data, "tb_district");

        public IReadOnlyDictionary<uint, IItemTableEntity> ReadItemTable() =>
            Read<uint, ItemTableEntity, IItemTableEntity>(_data, "tb_item");

        public IReadOnlyDictionary<ushort, ICharacterInfoTableEntity> ReadCharacterInfoTable() =>
            Read<ushort, CharacterInfoTableEntity, ICharacterInfoTableEntity>(_data, "tb_Character_Info");

        public IReadOnlyDictionary<uint, IPhotoItemTableEntity> ReadPhotoItemTable() =>
            Read<uint, PhotoItemTableEntity, IPhotoItemTableEntity>(_data, "tb_Photo_Item");

        public IReadOnlyDictionary<ushort, IGestureTableEntity> ReadGestureTable() =>
            Read<ushort, GestureTableEntity, IGestureTableEntity>(_data, "tb_Gesture");

        public BinTable(IConfiguration configuration) => _data = new(configuration);

        internal static IReadOnlyDictionary<TId, TItemInterface> Read<TId, TItem, TItemInterface>(VData12 data, string file) where TId : IConvertible where TItem : ITableEntity<TId> where TItemInterface : class, ITableEntity<TId> => GetEntries(data, file)
            .Select(br => (TItemInterface)Activator.CreateInstance(typeof(TItem), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { br }, null))
            .ToDictionary(k => k.Id, v => v);

        private static IEnumerable<BinaryReader> GetEntries(VData12 data, string file)
        {
            using BinaryReader br = new(data.GetInputStream(data.GetEntry($"../bin/Table/{file}.res")));

            for (uint q = 0, count = br.ReadUInt32(); q < count; ++q)
                yield return br;
        }
    }
}