using Microsoft.Extensions.Configuration;
using SoulCore;
using SoulCore.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Auth.Game.Repositories
{
    public sealed class GateRepository : List<GateRepository.Entity>
    {
        public sealed record Entity
        {
            public ushort Id { get; }
            public string Ip { get; set; } = default!;
            public ushort Port { get; set; }
            public string Name { get; set; } = default!;
            public GateStatus Status { get; set; }

            public Entity(InstanceConfiguration configuration)
            {
                Id = configuration.Id;
                Ip = configuration.Gate.Host.Ip;
                Port = configuration.Gate.Host.Port;
                Name = configuration.Gate.Name;
                Status = GateStatus.Online;
            }
        }

        public GateRepository(IConfiguration configuration) : base(GetGates(configuration))
        {
        }

        private static IEnumerable<Entity> GetGates(IConfiguration configuration) => configuration
            .GetSection("World").Get<WorldConfiguration>().Instance
            .Select(s => new Entity(s.Value));
    }
}
