using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Responses.Shared;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record AuthPersonalGateResponse
    {
        public sealed record GateInfo
        {
            public ushort Id { get; init; }
            public EndPointShared EndPoint { get; init; } = default!;
            public string Name { get; init; } = default!;
            public ushort PlayersOnlineCount { get; set; }
            public GateStatus Status { get; init; }
        }

        public GateInfo Gate { get; init; } = default!;
        public byte CharactersCount { get; init; }
    }
}
