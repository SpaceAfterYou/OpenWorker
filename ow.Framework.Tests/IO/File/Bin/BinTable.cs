using Microsoft.Extensions.DependencyInjection;
using ow.Framework.IO.File.Bin;
using Xunit;

namespace ow.Framework.Tests.IO.File.Bin
{
    public sealed class BinTableTest : IClassFixture<Startup>
    {
        private readonly BinTable _tables;

        public BinTableTest(Startup testSetup) => _tables = testSetup.ServiceProvider.GetRequiredService<BinTable>();

        [Fact]
        public void ReadClassSelectInfoTable() => _tables.ReadClassSelectInfoTable();

        [Fact]
        public void ReadCustomizeEyesTable() => _tables.ReadCustomizeEyesTable();

        [Fact]
        public void ReadCustomizeHairTable() => _tables.ReadCustomizeHairTable();

        [Fact]
        public void ReadCustomizeSkinTable() => _tables.ReadCustomizeSkinTable();

        [Fact]
        public void ReadDistrictTable() => _tables.ReadDistrictTable();

        [Fact]
        public void ReadItemTable() => _tables.ReadItemTable();

        [Fact]
        public void ReadCustomizeInfoTable() => _tables.ReadCustomizeInfoTable();

        [Fact]
        public void ReadCharacterBackgroundTable() => _tables.ReadCharacterBackgroundTable();

        [Fact]
        public void ReadItemClassifyTable() => _tables.ReadItemClassifyTable();

        [Fact]
        public void ReadItemScriptTable() => _tables.ReadItemScriptTable();

        [Fact]
        public void ReadCharacterInfoTable() => _tables.ReadCharacterInfoTable();

        [Fact]
        public void ReadPhotoItemTable() => _tables.ReadPhotoItemTable();

        [Fact]
        public void ReadGestureTable() => _tables.ReadGestureTable();

        [Fact]
        public void ReadMazeInfoTable() => _tables.ReadMazeInfoTable();

        [Fact]
        public void ReadBoosterTable() => _tables.ReadBoosterTable();

        [Fact]
        public void ReadNpcTable() => _tables.ReadNpcTable();

        [Fact]
        public void ReadPassInfo() => _tables.ReadPassInfoTable();

        [Fact]
        public void ReadPassRewardInfoTable() => _tables.ReadPassRewardInfoTable();
    }
}