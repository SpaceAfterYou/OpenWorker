using Microsoft.Extensions.Configuration;

namespace ow.Framework.IO.File.World
{
    public sealed class WorldFile : VisionDataFile
    {
        public WorldFile(IConfiguration configuration) : base(configuration, 49)
        {
        }
    }
}
