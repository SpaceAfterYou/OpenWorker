using Microsoft.Extensions.Configuration;

namespace ow.Service.Gate.Game
{
    public sealed class GateInfo
    {
        public ushort Id { get; init; }

        public GateInfo(IConfiguration configuration) =>
            Id = ushort.Parse(configuration["Id"]);
    }
}