using Microsoft.Extensions.Configuration;

namespace SoulCore.IO.File.World
{
    public sealed class WorldFile : VisionDataFile
    {
        public WorldFile(IConfiguration configuration) : base(configuration, 49)
        {
        }
    }
}
