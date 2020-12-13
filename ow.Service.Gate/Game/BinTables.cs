using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Service.Gate.Game
{
    internal sealed class BinTables : IBinTables
    {
        public IReadOnlyDictionary<Hero, IClassSelectInfoTableEntity> ClassSelectInfoTable { get; }
        public IReadOnlyDictionary<Hero, ICustomizeSkinTableEntity> CustomizeSkinTable { get; }
        public IReadOnlyDictionary<Hero, ICustomizeEyesTableEntity> CustomizeEyesTable { get; }
        public IReadOnlyDictionary<Hero, ICustomizeHairTableEntity> CustomizeHairTable { get; }
        public IReadOnlyDictionary<ushort, IDistrictTableEntity> DistrictTable { get; }
        public IReadOnlyDictionary<ushort, ICharacterInfoTableEntity> CharacterInfoTable { get; }
        public IReadOnlyDictionary<uint, ICharacterBackgroundTableEntity> CharacterBackgroundTable { get; }
        public IReadOnlyDictionary<uint, IPhotoItemTableEntity> PhotoItemTable { get; }

        public BinTables(BinTable processor)
        {
            ClassSelectInfoTable = processor.ReadClassSelectInfoTable();
            CustomizeSkinTable = processor.ReadCustomizeSkinTable();
            CustomizeEyesTable = processor.ReadCustomizeEyesTable();
            CustomizeHairTable = processor.ReadCustomizeHairTable();
            DistrictTable = processor.ReadDistrictTable();
            CharacterInfoTable = processor.ReadCharacterInfoTable();
            CharacterBackgroundTable = processor.ReadCharacterBackgroundTable();
            PhotoItemTable = processor.ReadPhotoItemTable();
        }
    }
}