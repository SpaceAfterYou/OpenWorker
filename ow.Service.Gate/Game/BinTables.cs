using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Service.Gate.Game
{
    internal sealed class BinTables
    {
        internal IReadOnlyDictionary<Hero, IClassSelectInfoTableEntity> ClassSelectInfoTable { get; }
        internal IReadOnlyDictionary<Hero, ICustomizeSkinTableEntity> CustomizeSkinTable { get; }
        internal IReadOnlyDictionary<Hero, ICustomizeEyesTableEntity> CustomizeEyesTable { get; }
        internal IReadOnlyDictionary<Hero, ICustomizeHairTableEntity> CustomizeHairTable { get; }
        internal IReadOnlyDictionary<ushort, IDistrictTableEntity> DistrictTable { get; }
        internal IReadOnlyDictionary<ushort, ICharacterInfoTableEntity> CharacterInfoTable { get; }
        internal IReadOnlyDictionary<uint, ICharacterBackgroundTableEntity> CharacterBackgroundTable { get; }
        internal IReadOnlyDictionary<uint, IPhotoItemTableEntity> PhotoItemTable { get; }

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