using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.QuickSlot
{
    [Request]
    public readonly struct SetSkillsRequest
    {
        public QuickSlotColumn[] Column { get; }

        public SetSkillsRequest(BinaryReader br)
        {
            var count = br.ReadByte();
            Column = new QuickSlotColumn[count];

            for (byte q = 0; q != count; ++q)
            {
                Column[q] = new QuickSlotColumn(br);
            }
        }
    }

    public readonly struct QuickSlotColumn
    {
        public ushort Id { get; }
        public QuickSlot[] Row { get; }
        public uint Unknown1 { get; }

        public QuickSlotColumn(BinaryReader br)
        {
            Id = br.ReadUInt16();

            Row = new QuickSlot[3]
            {
                new QuickSlot(br),
                new QuickSlot(br),
                new QuickSlot(br)
            };

            Unknown1 = br.ReadUInt32();
        }
    }

    public readonly struct QuickSlot
    {
        public uint Id { get; }

        public QuickSlot(BinaryReader br)
            => Id = br.ReadUInt32();
    }
}
