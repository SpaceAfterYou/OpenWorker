using Microsoft.Extensions.Configuration;
using System.Net;

namespace GateService.Game
{
    public sealed class Gate : IPEndPoint
    {
        public ushort Id { get; init; }

        public Gate(IConfiguration configuration) : base(IPAddress.Parse(configuration["Host:Ip"]), ushort.Parse(configuration["Host:Port"])) =>
            Id = ushort.Parse(configuration["Id"]);
    }
}
