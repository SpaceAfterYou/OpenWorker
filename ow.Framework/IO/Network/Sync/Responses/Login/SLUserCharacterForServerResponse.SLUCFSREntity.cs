using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Responses.Shared;

namespace ow.Framework.IO.Network.Sync.Responses
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