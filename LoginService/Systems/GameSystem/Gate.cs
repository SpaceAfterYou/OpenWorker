using Microsoft.Extensions.Configuration;
using SoulWorker.Types;
using System.Net;

namespace LoginService.Systems.GameSystem
{
    public sealed class Gate
    {
        public ushort Id { get; init; }
        public IPEndPoint EndPoint { get; init; }
        public string Name { get; init; }
        public ushort PlayersOnlineCount { get; init; }
        public GateStatusType Status { get; init; }

        public Gate(IConfigurationSection section)
        {
            Id = ushort.Parse(section["Id"]);
            EndPoint = new(IPAddress.Parse(section["Host:Ip"]), ushort.Parse(section["Host:Port"]));
            Name = section["Name"];
            PlayersOnlineCount = 0;
            Status = GateStatusType.Online;
        }
    }
}