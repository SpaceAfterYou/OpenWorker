using Microsoft.Extensions.Configuration;
using ow.Framework;
using ow.Framework.Game.Enums;
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

            public Entity(ushort id, GateConfiguration request)
            {
                Id = id;
                Ip = request.Host.Ip;
                Port = request.Host.Port;
                Name = request.Name;
                Status = GateStatus.Online;
            }
        }

        public GateRepository(IConfiguration configuration) : base(GetGates(configuration))
        {
        }

        private static IEnumerable<Entity> GetGates(IConfiguration configuration) => configuration
            .GetSection("Gates").Get<IReadOnlyDictionary<string, GateConfiguration>>()
            .Select(c => new Entity(ushort.Parse(c.Key), c.Value));
    }
}