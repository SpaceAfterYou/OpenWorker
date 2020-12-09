using System.Collections.Generic;
using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.QuickSlot
{
    [Request]
    public readonly struct SetItemsRequest
    {
        public IReadOnlyList<int> Ids { get; }

        public SetItemsRequest(BinaryReader br) =>
            Ids = new int[] { br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadInt32() };
    }
}
