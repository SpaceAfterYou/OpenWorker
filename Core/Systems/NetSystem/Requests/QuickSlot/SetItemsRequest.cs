using System.Collections.Generic;
using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.QuickSlot
{
    [Request]
    public readonly struct SetItemsRequest
    {
        public IReadOnlyList<int> Ids { get; }

        public SetItemsRequest(BinaryReader br) =>
            Ids = new int[] { br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadInt32() };
    }
}
