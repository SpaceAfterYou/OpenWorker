using Microsoft.Extensions.Configuration;

namespace ow.Service.Gate.Game
{
    public sealed record GateInstance
    {
        public ushort Id { get; }

        public GateInstance(IConfiguration configuration) => Id = ushort.Parse(configuration["Id"]);
    }
}