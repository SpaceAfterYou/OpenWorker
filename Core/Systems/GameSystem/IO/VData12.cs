using Microsoft.Extensions.Configuration;

namespace Core.Systems.GameSystem.IO
{
    public sealed class VData12 : VData
    {
        public VData12(IConfiguration configuration) : base(configuration, 12)
        {
        }
    }
}