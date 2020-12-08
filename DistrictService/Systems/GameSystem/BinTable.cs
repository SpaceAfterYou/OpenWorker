using Core.Systems.GameSystem;
using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using SoulWorker.Types;
using System.Collections.Generic;

namespace DistrictService.Systems.GameSystem
{
    public sealed class BinTable
    {
        internal IReadOnlyDictionary<HeroType, CustomizeSkinTableEntity> CustomizeSkinTable { get; }

        internal IReadOnlyDictionary<HeroType, CustomizeEyesTableEntity> CustomizeEyesTable { get; }

        internal IReadOnlyDictionary<HeroType, CustomizeHairTableEntity> CustomizeHairTable { get; }

        internal IReadOnlyDictionary<ushort, DistrictTableEntity> DistrictTable { get; }

        public BinTable(BinTableProcessor binTableProcessor)
        {
            CustomizeSkinTable = binTableProcessor.ReadCustomizeSkinTable();
            CustomizeEyesTable = binTableProcessor.ReadCustomizeEyesTable();
            CustomizeHairTable = binTableProcessor.ReadCustomizeHairTable();
            DistrictTable = binTableProcessor.ReadDistrictTable();
        }
    }
}