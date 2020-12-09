using ow.Framework.Game.Datas.Bin;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Ids;
using ow.Framework.IO.GameFile;
using System.Collections.Generic;

namespace ow.Framework.Game
{
    public class BinTable
    {
        public IReadOnlyDictionary<HeroId, ClassSelectInfoTableEntity> ReadClassSelectInfoTable() =>
            TableReader<HeroId, ClassSelectInfoTableEntity>.Read(_data, "tb_ClassSelect_Info");

        public IReadOnlyDictionary<HeroId, CustomizeSkinTableEntity> ReadCustomizeSkinTable() =>
            TableReader<HeroId, CustomizeSkinTableEntity>.Read(_data, "tb_Customize_Skin");

        public IReadOnlyDictionary<HeroId, CustomizeEyesTableEntity> ReadCustomizeEyesTable() =>
            TableReader<HeroId, CustomizeEyesTableEntity>.Read(_data, "tb_Customize_Eyes");

        public IReadOnlyDictionary<HeroId, CustomizeHairTableEntity> ReadCustomizeHairTable() =>
            TableReader<HeroId, CustomizeHairTableEntity>.Read(_data, "tb_Customize_Hair");

        public IReadOnlyDictionary<ushort, DistrictTableEntity> ReadDistrictTable() =>
            TableReader<ushort, DistrictTableEntity>.Read(_data, "tb_district");

        public IReadOnlyDictionary<uint, ItemTableEntity> ReadItemTable() =>
            TableReader<uint, ItemTableEntity>.Read(_data, "tb_item");

        public IReadOnlyDictionary<ushort, CharacterInfoTableEntity> ReadCharacterInfoTable() =>
            TableReader<ushort, CharacterInfoTableEntity>.Read(_data, "tb_Character_Info");

        public BinTable(IConfiguration configuration) => _data = new(configuration);

        private readonly VData12 _data;
    }
}
