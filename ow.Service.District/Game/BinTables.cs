using Microsoft.Extensions.Configuration;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Service.District.Game
{
    public sealed class BinTables : IBinTables
    {
        public IReadOnlyDictionary<Hero, CustomizeSkinTableEntity> CustomizeSkinTable { get; }
        public IReadOnlyDictionary<Hero, CustomizeEyesTableEntity> CustomizeEyesTable { get; }
        public IReadOnlyDictionary<Hero, CustomizeHairTableEntity> CustomizeHairTable { get; }
        public IReadOnlyDictionary<ushort, DistrictTableEntity> DistrictTable { get; }
        public IReadOnlyDictionary<ushort, MazeInfoTableEntity> MazeInfoTable { get; }
        public IReadOnlyDictionary<ushort, BoosterTableEntity> BoosterTable { get; }

        public BinTables(IConfiguration configuration)
        {
            using BinTable tables = new(configuration);

            CustomizeSkinTable = tables.ReadCustomizeSkinTable();
            CustomizeEyesTable = tables.ReadCustomizeEyesTable();
            CustomizeHairTable = tables.ReadCustomizeHairTable();
            DistrictTable = tables.ReadDistrictTable();
            MazeInfoTable = tables.ReadMazeInfoTable();
            BoosterTable = tables.ReadBoosterTable();
        }
    }
}