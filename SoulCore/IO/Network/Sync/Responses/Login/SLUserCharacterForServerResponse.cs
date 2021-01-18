using System;
using System.Collections.Generic;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed partial record SLUserCharacterForServerResponse
    {
        public byte LastSelectedId { get; init; }
        public IEnumerable<SLUCFSREntity> Values { get; init; } = Array.Empty<SLUCFSREntity>();
    }
}
