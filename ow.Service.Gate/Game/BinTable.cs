using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Ids;
using System.Collections.Generic;

namespace ow.Service.Gate.Game
{
    internal sealed class BinTable
    {
        internal IReadOnlyDictionary<HeroId, ClassSelectInfoTableEntity> ClassSelectInfoTable { get; }
        internal IReadOnlyDictionary<HeroId, CustomizeSkinTableEntity> CustomizeSkinTable { get; }
        internal IReadOnlyDictionary<HeroId, CustomizeEyesTableEntity> CustomizeEyesTable { get; }
        internal IReadOnlyDictionary<HeroId, CustomizeHairTableEntity> CustomizeHairTable { get; }
        internal IReadOnlyDictionary<ushort, DistrictTableEntity> DistrictTable { get; }
        internal IReadOnlyDictionary<ushort, CharacterInfoTableEntity> CharacterInfoTable { get; }
        internal IReadOnlyDictionary<uint, CharacterBackgroundTableEntity> CharacterBackgroundTable { get; }

        public BinTable(BinTableProcessor binTable)
        {
            ClassSelectInfoTable = binTable.ReadClassSelectInfoTable();
            CustomizeSkinTable = binTable.ReadCustomizeSkinTable();
            CustomizeEyesTable = binTable.ReadCustomizeEyesTable();
            CustomizeHairTable = binTable.ReadCustomizeHairTable();
            DistrictTable = binTable.ReadDistrictTable();
            CharacterInfoTable = binTable.ReadCharacterInfoTable();
            CharacterBackgroundTable = binTable.ReadCharacterBackgroundTable();
        }
    }
}