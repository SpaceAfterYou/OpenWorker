using Microsoft.Extensions.Configuration;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Service.Gate.Game
{
    internal sealed class BinTables : IBinTables
    {
        public IReadOnlyDictionary<Hero, ClassSelectInfoTableEntity> ClassSelectInfoTable { get; }
        public IReadOnlyDictionary<Hero, CustomizeSkinTableEntity> CustomizeSkinTable { get; }
        public IReadOnlyDictionary<Hero, CustomizeEyesTableEntity> CustomizeEyesTable { get; }
        public IReadOnlyDictionary<Hero, CustomizeHairTableEntity> CustomizeHairTable { get; }
        public IReadOnlyDictionary<ushort, DistrictTableEntity> DistrictTable { get; }
        public IReadOnlyDictionary<ushort, CharacterInfoTableEntity> CharacterInfoTable { get; }
        public IReadOnlyDictionary<uint, CharacterBackgroundTableEntity> CharacterBackgroundTable { get; }
        public IReadOnlyDictionary<uint, PhotoItemTableEntity> PhotoItemTable { get; }

        public BinTables(IConfiguration configuration)
        {
            using BinTable tables = new(configuration);

            ClassSelectInfoTable = tables.ReadClassSelectInfoTable();
            CustomizeSkinTable = tables.ReadCustomizeSkinTable();
            CustomizeEyesTable = tables.ReadCustomizeEyesTable();
            CustomizeHairTable = tables.ReadCustomizeHairTable();
            DistrictTable = tables.ReadDistrictTable();
            CharacterInfoTable = tables.ReadCharacterInfoTable();
            CharacterBackgroundTable = tables.ReadCharacterBackgroundTable();
            PhotoItemTable = tables.ReadPhotoItemTable();
        }
    }
}