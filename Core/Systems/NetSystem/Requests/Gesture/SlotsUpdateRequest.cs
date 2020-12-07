using Core.Systems.NetSystem.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Systems.NetSystem.Requests.Gesture
{
    public readonly struct SlotsUpdateSlotRequest
    {
        public int Id { get; }
        public uint Gesture { get; }

        public SlotsUpdateSlotRequest(int slot, uint gesture) => (Id, Gesture) = (slot, gesture);
    }

    [Request]
    public readonly struct SlotsUpdateRequest
    {
        public IEnumerable<SlotsUpdateSlotRequest> Slots { get; }

        public SlotsUpdateRequest(BinaryReader br) =>
            Slots = Enumerable
                .Range(0, Constants.GesturesCount)
                .Select(id => new SlotsUpdateSlotRequest(id, br.ReadUInt32()));
    }
}