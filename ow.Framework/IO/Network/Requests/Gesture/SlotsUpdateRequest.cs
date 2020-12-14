using ow.Framework.IO.Network.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ow.Framework.IO.Network.Requests.Gesture
{
    public readonly struct SlotsUpdateSlotRequest
    {
        public int Id { get; }
        public uint Value { get; }

        public SlotsUpdateSlotRequest(int slot, uint gesture) => (Id, Value) = (slot, gesture);
    }

    [Request]
    public readonly struct SlotsUpdateRequest
    {
        public IEnumerable<SlotsUpdateSlotRequest> Values { get; }

        public SlotsUpdateRequest(BinaryReader br) => Values = Enumerable
            .Range(0, Defines.QuickSlotsCount)
            .Select(id => new SlotsUpdateSlotRequest(id, br.ReadUInt32()));
    }
}