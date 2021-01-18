using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Responses.Shared;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed partial record SLUserCharacterForServerResponse
    {
        public readonly struct SLUCFSREntity
        {
            public ushort Id { get; init; }
            public SEndPointSharedResponse EndPoint { get; init; }
            public string Name { get; init; }
            public GateStatus Status { get; init; }
            public uint PlayersOnlineCount { get; init; }
            public byte CharactersCount { get; init; }
        }
    }
}
