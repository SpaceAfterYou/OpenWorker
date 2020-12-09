using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Ids;
using System.Collections.Generic;

namespace ow.Service.District.Game
{
    public sealed class BinTable
    {
        internal IReadOnlyDictionary<HeroId, CustomizeSkinTableEntity> CustomizeSkinTable { get; }
        internal IReadOnlyDictionary<HeroId, CustomizeEyesTableEntity> CustomizeEyesTable { get; }
        internal IReadOnlyDictionary<HeroId, CustomizeHairTableEntity> CustomizeHairTable { get; }
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
