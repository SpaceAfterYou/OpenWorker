using Microsoft.Extensions.Configuration;
using ow.Framework;
using ow.Service.World.Network.Relay;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.World.Game.Gate.Repository
{
    public sealed class DistrictRepository : Dictionary<ushort, DistrictRepository.Entity>
    {
        public sealed record Entity
        {
            internal string Ip { get; }
            internal ushort Port { get; }
            internal WorldRelayClient Relay { get; }

            internal Entity(string district, DistrictConfiguration configuration1, IConfiguration configuration)
            {
                Ip = configuration1.Host.Ip;
                Port = configuration1.Host.Port;
                Relay = new(configuration.GetSection($"World:Instance:{configuration["World"]}:District:{district}:Relay:World:Host"));
            }
        }

        public DistrictRepository(IConfiguration configuration) : base(GetEntities(configuration))
        {
        }

        private static IEnumerable<KeyValuePair<ushort, Entity>> GetEntities(IConfiguration configuration) => configuration
            .GetSection($"World:Instance:{configuration["World"]}").Get<InstanceConfiguration>().District
            .Select(s => KeyValuePair.Create(s.Value.Location, new Entity(s.Key, s.Value, configuration)));
    }
}

// https://youtu.be/8n11Rv6RCeM