using Microsoft.Extensions.Configuration;
using SoulWorker.Types;

namespace LoginService.Systems.GameSystem
{
    public sealed class Gate
    {
        public ushort Id { get; init; }
        public string Ip { get; init; }
        public ushort Port { get; init; }
        public string Name { get; init; }
        public ushort PlayersOnlineCount { get; set; }
        public GateStatusType Status { get; init; }

        public Gate(IConfigurationSection section)
        {
            Id = ushort.Parse(section["Id"]);
            Ip = section["Host:Ip"];
            Port = ushort.Parse(section["Host:Port"]);
            Name = section["Name"];
            PlayersOnlineCount = 0;
            Status = GateStatusType.Online;
        }
    }
}