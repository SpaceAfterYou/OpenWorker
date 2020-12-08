using Core.Systems.GameSystem;
using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using SoulWorker.Types;
using System.Collections.Generic;

namespace GateService.Systems.GameSystem
{
    internal sealed class BinTable
    {
        internal IReadOnlyDictionary<HeroType, ClassSelectInfoTableEntity> ClassSelectInfoTable { get; }
        internal IReadOnlyDictionary<HeroType, CustomizeSkinTableEntity> CustomizeSkinTable { get; }
        internal IReadOnlyDictionary<HeroType, CustomizeEyesTableEntity> CustomizeEyesTable { get; }
        internal IReadOnlyDictionary<HeroType, CustomizeHairTableEntity> CustomizeHairTable { get; }
        internal IReadOnlyDictionary<ushort, DistrictTableEntity> DistrictTable { get; }
        internal IReadOnlyDictionary<ushort, CharacterInfoTableEntity> CharacterInfoTable { get; }

        public BinTable(BinTableProcessor binTableProcessor)
        {
            ClassSelectInfoTable = binTableProcessor.ReadClassSelectInfoTable();
            CustomizeSkinTable = binTableProcessor.ReadCustomizeSkinTable();
            CustomizeEyesTable = binTableProcessor.ReadCustomizeEyesTable();
            CustomizeHairTable = binTableProcessor.ReadCustomizeHairTable();
            DistrictTable = binTableProcessor.ReadDistrictTable();
            CharacterInfoTable = binTableProcessor.ReadCharacterInfoTable();
        }
    }
}