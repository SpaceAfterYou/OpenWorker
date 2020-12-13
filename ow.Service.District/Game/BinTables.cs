using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Service.District.Game
{
    public sealed class BinTables
    {
        internal IReadOnlyDictionary<Hero, ICustomizeSkinTableEntity> CustomizeSkinTable { get; }
        internal IReadOnlyDictionary<Hero, ICustomizeEyesTableEntity> CustomizeEyesTable { get; }
        internal IReadOnlyDictionary<Hero, ICustomizeHairTableEntity> CustomizeHairTable { get; }
        internal IReadOnlyDictionary<ushort, IDistrictTableEntity> DistrictTable { get; }

        public BinTables(BinTable binTableProcessor)
        {
            CustomizeSkinTable = binTableProcessor.ReadCustomizeSkinTable();
            CustomizeEyesTable = binTableProcessor.ReadCustomizeEyesTable();
            CustomizeHairTable = binTableProcessor.ReadCustomizeHairTable();
            DistrictTable = binTableProcessor.ReadDistrictTable();
        }
    }
}