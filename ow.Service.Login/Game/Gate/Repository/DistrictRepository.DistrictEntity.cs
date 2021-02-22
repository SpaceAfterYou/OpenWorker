using Microsoft.Extensions.Configuration;
using ow.Core.World.Network.Relay;
using ow.Framework.Configuration;

namespace ow.Service.Login.Game.Gate.Repository
{
    public sealed partial class DistrictRepository
    {
        public sealed record DistrictEntity
        {
            internal readonly string Ip;
            internal readonly ushort Port;
            internal readonly RWClient Relay;

            internal DistrictEntity(string district, DistrictConfiguration configuration1, IConfiguration configuration)
            {
                Ip = configuration1.Host.Ip;
                Port = configuration1.Host.Port;
                Relay = new(configuration.GetSection($"World:Instance:{configuration["World"]}:District:{district}:Relay:World:Host"));
            }
        }
    }
}

// https://youtu.be/8n11Rv6RCeM