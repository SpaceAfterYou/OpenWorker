using Core.Systems.GameSystem.Datas.Bin;
using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using Core.Systems.GameSystem.IO;
using Microsoft.Extensions.Configuration;
using SoulWorker.Types;
using System.Collections.Generic;

namespace Core.Systems.GameSystem
{
    public class BinTableProcessor
    {
        public IReadOnlyDictionary<HeroType, ClassSelectInfoTableEntity> ReadClassSelectInfoTable() =>
            TableReader<HeroType, ClassSelectInfoTableEntity>.Read(_data, "tb_ClassSelect_Info");

        public IReadOnlyDictionary<HeroType, CustomizeSkinTableEntity> ReadCustomizeSkinTable() =>
            TableReader<HeroType, CustomizeSkinTableEntity>.Read(_data, "tb_Customize_Skin");

        public IReadOnlyDictionary<HeroType, CustomizeEyesTableEntity> ReadCustomizeEyesTable() =>
                TableReader<HeroType, CustomizeEyesTableEntity>.Read(_data, "tb_Customize_Eyes");

        public IReadOnlyDictionary<HeroType, CustomizeHairTableEntity> ReadCustomizeHairTable() =>
            TableReader<HeroType, CustomizeHairTableEntity>.Read(_data, "tb_Customize_Hair");

        public IReadOnlyDictionary<ushort, DistrictTableEntity> ReadDistrictTable() =>
            TableReader<ushort, DistrictTableEntity>.Read(_data, "tb_district");

        public IReadOnlyDictionary<uint, ItemTableEntity> ReadItemTable() =>
            TableReader<uint, ItemTableEntity>.Read(_data, "tb_item");

        public IReadOnlyDictionary<ushort, CharacterInfoTableEntity> ReadCharacterInfoTable() =>
            TableReader<ushort, CharacterInfoTableEntity>.Read(_data, "tb_Character_Info");

        public BinTableProcessor(IConfiguration configuration) => _data = new(configuration);

        private readonly VData12 _data;
    }
}