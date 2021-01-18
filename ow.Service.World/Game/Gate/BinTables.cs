using Microsoft.Extensions.Configuration;
using SoulCore.Game.Datas.Bin.Table.Entities;
using SoulCore.Game.Enums;
using SoulCore.IO.File.Bin;
using System.Collections.Generic;

namespace ow.Service.World.Game.Gate
{
    public sealed record BinTables
    {
        internal IReadOnlyDictionary<Hero, ClassSelectInfoTableEntity> ClassSelectInfo { get; }
        internal IReadOnlyDictionary<Hero, CustomizeSkinTableEntity> CustomizeSkin { get; }
        internal IReadOnlyDictionary<Hero, CustomizeEyesTableEntity> CustomizeEyes { get; }
        internal IReadOnlyDictionary<Hero, CustomizeHairTableEntity> CustomizeHair { get; }
        internal IReadOnlyDictionary<ushort, CharacterInfoTableEntity> CharacterInfo { get; }
        internal IReadOnlyDictionary<uint, CharacterBackgroundTableEntity> CharacterBackground { get; }
        internal IReadOnlyDictionary<uint, PhotoItemTableEntity> PhotoItem { get; }
        internal IReadOnlyDictionary<uint, ItemTableEntity> Item { get; }
        internal IReadOnlyDictionary<uint, ItemClassifyTableEntity> ItemClassify { get; }

        public BinTables(IConfiguration configuration)
        {
            using BinTable tables = new(configuration);

            ClassSelectInfo = tables.ReadClassSelectInfoTable();
            CustomizeSkin = tables.ReadCustomizeSkinTable();
            CustomizeEyes = tables.ReadCustomizeEyesTable();
            CustomizeHair = tables.ReadCustomizeHairTable();
            CharacterInfo = tables.ReadCharacterInfoTable();
            CharacterBackground = tables.ReadCharacterBackgroundTable();
            PhotoItem = tables.ReadPhotoItemTable();
            Item = tables.ReadItemTable();
            ItemClassify = tables.ReadItemClassifyTable();
        }
    }
}
