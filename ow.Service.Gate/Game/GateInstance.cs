using Microsoft.Extensions.Configuration;

namespace ow.Service.Gate.Game
{
    public sealed class GateInstance
    {
        public ushort Id { get; init; }

        public GateInstance(IConfiguration configuration) => Id = ushort.Parse(configuration["Id"]);
    }
}