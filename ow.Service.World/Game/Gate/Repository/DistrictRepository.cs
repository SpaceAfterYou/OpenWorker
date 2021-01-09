using Microsoft.Extensions.Configuration;
using ow.Framework;
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

            internal Entity(DistrictConfiguration configuration) =>
                (Ip, Port) = (configuration.Host.Ip, configuration.Host.Port);
        }

        public DistrictRepository(IConfiguration configuration) : base(GetEntities(configuration))
        {
        }

        private static IEnumerable<KeyValuePair<ushort, Entity>> GetEntities(IConfiguration configuration) => configuration
            .GetSection($"World:Instance:{configuration["World"]}").Get<InstanceConfiguration>().District
            .Select(s => KeyValuePair.Create(s.Value.Location, new Entity(s.Value)));
    }
}

// https://youtu.be/8n11Rv6RCeM