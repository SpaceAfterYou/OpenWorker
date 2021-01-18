using Microsoft.Extensions.Configuration;

namespace SoulCore.IO.File.Bin
{
    internal sealed class BinFile : VisionDataFile
    {
        internal BinFile(IConfiguration configuration) : base(configuration, 12)
        {
        }
    }
}
