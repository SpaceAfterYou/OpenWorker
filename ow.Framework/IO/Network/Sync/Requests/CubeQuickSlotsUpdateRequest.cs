using ow.Framework.IO.Network.Sync.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct CubeQuickSlotsUpdateRequest
    {
        public IReadOnlyList<int> Values { get; }

        public CubeQuickSlotsUpdateRequest(BinaryReader br) => Values = Enumerable.Range(0, Defines.CubeQuickSlotCount).Select(_ => br.ReadInt32()).ToArray();
    }
}
