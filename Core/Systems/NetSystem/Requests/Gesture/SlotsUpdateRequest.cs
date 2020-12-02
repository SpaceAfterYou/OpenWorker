using Core.Systems.NetSystem.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core.Systems.NetSystem.Requests.Gesture
{
    public readonly struct Slot
    {
        public int Id { get; }
        public uint Gesture { get; }

        public Slot(int slot, uint gesture) => (Id, Gesture) = (slot, gesture);
    }

    [Request]
    public readonly struct SlotsUpdateRequest
    {
        public IEnumerable<Slot> Slots { get; }

        public SlotsUpdateRequest(BinaryReader br) =>
            Slots = Enumerable
                .Range(0, SoulWorker.Constants.GesturesCount)
                .Select(id => new Slot(id, br.ReadUInt32()));
    }
}
