using Core.Systems.GameSystem;
using Core.Systems.GameSystem.Datas.World.Table;
using Microsoft.Extensions.Configuration;

namespace DistrictService.Systems.GameSystem
{
    public class Zone
    {
        public VRoot Place { get; }

        public Zone(IConfiguration configuration)
        {
            Place = WorldTableProcessor.Read(configuration, "");
        }
    }
}