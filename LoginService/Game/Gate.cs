using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Statuses;

namespace LoginService.Game
{
    public sealed class Gate
    {
        public ushort Id { get; init; }
        public string Ip { get; init; }
        public ushort Port { get; init; }
        public string Name { get; init; }
        public ushort PlayersOnlineCount { get; set; }
        public GateStatus Status { get; init; }

        public Gate(IConfigurationSection section)
        {
            Id = ushort.Parse(section["Id"]);
            Ip = section["Host:Ip"];
            Port = ushort.Parse(section["Host:Port"]);
            Name = section["Name"];
            PlayersOnlineCount = 0;
            Status = GateStatus.Online;
        }
    }
}