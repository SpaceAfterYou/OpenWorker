using Microsoft.Extensions.DependencyInjection;
using ow.Framework.Game;
using Xunit;

namespace ow.Framework.Tests.IO.GameFile
{
    public sealed class BinTableTest : IClassFixture<Startup>
    {
        private readonly BinTableProcessor _binTableProcessor;

        public BinTableTest(Startup testSetup) =>
            _binTableProcessor = testSetup.ServiceProvider.GetRequiredService<BinTableProcessor>();

        [Fact]
        public void ReadClassSelectInfoTable() =>
            _binTableProcessor.ReadClassSelectInfoTable();

        [Fact]
        public void ReadCustomizeEyesTable() =>
            _binTableProcessor.ReadCustomizeEyesTable();

        [Fact]
        public void ReadCustomizeHairTable() =>
            _binTableProcessor.ReadCustomizeHairTable();

        [Fact]
        public void ReadCustomizeSkinTable() =>
            _binTableProcessor.ReadCustomizeSkinTable();

        [Fact]
        public void ReadDistrictTable() =>
            _binTableProcessor.ReadDistrictTable();

        [Fact]
        public void ReadItemTable() =>
            _binTableProcessor.ReadItemTable();

        [Fact]
        public void ReadCustomizeInfoTable() =>
            _binTableProcessor.ReadCustomizeInfoTable();
    }
}