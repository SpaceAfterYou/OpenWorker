using Microsoft.Extensions.Configuration;
using SoulCore;
using System.Linq;

namespace ow.Service.World.Game.Gate
{
    public sealed record Instance
    {
        public ushort Id { get; }

        public Instance(IConfiguration configuration) => Id = GetId(configuration, configuration["World"]);

        private static ushort GetId(IConfiguration configuration, string id) => configuration
            .GetSection("World")
            .Get<WorldConfiguration>().Instance
            .Select((s, i) => new { Index = (ushort)i, Value = s })
            .First(s => s.Value.Key == id)
            .Index;
    }
}

// https://youtu.be/TfTXXWsy9No
