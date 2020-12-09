using Microsoft.Extensions.DependencyInjection;
using ow.Framework.Game;
using Xunit;

namespace ow.Framework.Tests.IO.GameFile
{
    public class BinTableTest : IClassFixture<Startup>
    {
        private ServiceProvider _serviceProvider;
        private BinTableProcessor _binTableProcessor;

        public BinTableTest(Startup testSetup)
        {
            _serviceProvider = testSetup.ServiceProvider;
            _binTableProcessor = _serviceProvider.GetRequiredService<BinTableProcessor>();
        }

        [Fact]
        public void ReadClassSelectInfoTable()
        {
            _binTableProcessor.ReadClassSelectInfoTable();
        }

        [Fact]
        public void ReadCustomizeEyesTable()
        {
            _binTableProcessor.ReadCustomizeEyesTable();
        }

        [Fact]
        public void ReadCustomizeHairTable()
        {
            _binTableProcessor.ReadCustomizeHairTable();
        }

        [Fact]
        public void ReadCustomizeSkinTable()
        {
            _binTableProcessor.ReadCustomizeSkinTable();
        }

        [Fact]
        public void ReadDistrictTable()
        {
            _binTableProcessor.ReadDistrictTable();
        }

        [Fact]
        public void ReadItemTable()
        {
            _binTableProcessor.ReadItemTable();
        }

        [Fact]
        public void ReadCustomizeInfoTable()
        {
            _binTableProcessor.ReadCustomizeInfoTable();
        }
    }
}