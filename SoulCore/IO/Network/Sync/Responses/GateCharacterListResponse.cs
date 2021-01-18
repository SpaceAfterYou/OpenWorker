using SoulCore.IO.Network.Sync.Responses.Shared;
using System.Collections.Generic;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record GateCharacterListResponse
    {
        public IReadOnlyList<CharacterShared> Characters { get; init; } = default!;
        public ulong InitializeTime { get; init; }
        public int LastSelected { get; init; }
    }
}
