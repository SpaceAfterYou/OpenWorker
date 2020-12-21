using ow.Framework;
using ow.Framework.Game.Enums;

namespace ow.Service.Login.Game
{
    public sealed class GateInstance
    {
        public ushort Id { get; }
        public string Ip { get; }
        public ushort Port { get; }
        public string Name { get; }
        public ushort PlayersOnlineCount { get; set; }
        public GateStatus Status { get; }

        public GateInstance(GateConfiguration configuration)
        {
            Id = configuration.Id;
            Ip = configuration.Host.Ip;
            Port = configuration.Host.Port;
            Name = configuration.Name;
            PlayersOnlineCount = 0;
            Status = GateStatus.Online;
        }
    }
}