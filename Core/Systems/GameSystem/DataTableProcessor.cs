using Core.Systems.GameSystem.Datas.Bin;
using Core.Systems.GameSystem.Datas.Bin.Table;
using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using Core.Systems.GameSystem.IO;
using Microsoft.Extensions.Configuration;
using SoulWorker.Types;

namespace Core.Systems.GameSystem
{
    public class BinTableProcessor
    {
        public IClassSelectInfoTable ReadClassSelectInfoTable() =>
            TableReader<HeroType, ClassSelectInfoTableEntity, IClassSelectInfoTable>.Read(_data, "tb_ClassSelect_Info");

        public ICustomizeSkinTable ReadCustomizeSkinTable() =>
            TableReader<HeroType, CustomizeSkinTableEntity, ICustomizeSkinTable>.Read(_data, "tb_Customize_Skin");

        public ICustomizeEyesTable ReadCustomizeEyesTable() =>
                TableReader<HeroType, CustomizeEyesTableEntity, ICustomizeEyesTable>.Read(_data, "tb_Customize_Eyes");

        public ICustomizeHairTable ReadCustomizeHairTable() =>
            TableReader<HeroType, CustomizeHairTableEntity, ICustomizeHairTable>.Read(_data, "tb_Customize_Hair");

        public IDistrictTable ReadDistrictTable() =>
            TableReader<ushort, DistrictTableEntity, IDistrictTable>.Read(_data, "tb_district");

        public IItemTable ReadItemTable() =>
            TableReader<uint, ItemTableEntity, IItemTable>.Read(_data, "tb_item");

        public BinTableProcessor(IConfiguration configuration) => _data = new(configuration);

        private readonly VData12 _data;
    }
}