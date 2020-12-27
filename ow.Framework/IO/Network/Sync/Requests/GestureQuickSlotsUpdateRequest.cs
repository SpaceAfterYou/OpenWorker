using ow.Framework.IO.Network.Sync.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct GestureQuickSlotsUpdateRequest
    {
        public IEnumerable<uint> Values { get; }

        public GestureQuickSlotsUpdateRequest(BinaryReader br) => Values = Enumerable.Range(0, Defines.QuickSlotsCount).Select(id => br.ReadUInt32());
    }
}
