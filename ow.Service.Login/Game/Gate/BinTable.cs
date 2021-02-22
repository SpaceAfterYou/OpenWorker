using Microsoft.Extensions.Configuration;
using ow.Framework.IO.File.Bin;
using SoulCore.Data.Bin.Table.Entities;
using SoulCore.Types;
using System.Collections.Generic;

namespace ow.Service.Login.Game.Gate
{
    public sealed record BinTable
    {
        internal readonly IReadOnlyDictionary<Class, ClassSelectInfoEntity> ClassSelectInfo;
        internal readonly IReadOnlyDictionary<Class, CustomizeSkinEntity> CustomizeSkin;
        internal readonly IReadOnlyDictionary<Class, CustomizeEyesEntity> CustomizeEyes;
        internal readonly IReadOnlyDictionary<Class, CustomizeHairEntity> CustomizeHair;
        internal readonly IReadOnlyDictionary<ushort, CharacterInfoEntity> CharacterInfo;
        internal readonly IReadOnlyDictionary<uint, CharacterBackgroundEntity> CharacterBackground;
        internal readonly IReadOnlyDictionary<uint, PhotoItemEntity> PhotoItem;
        internal readonly IReadOnlyDictionary<uint, ItemEntity> Item;
        internal readonly IReadOnlyDictionary<uint, ItemClassifyEntity> ItemClassify;

        public BinTable(IConfiguration configuration)
        {
            using BinReader tables = new(configuration);

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