using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct DistrictEnterRequest
    {
        public int Account { get; } // 6D 38 08 00
        public int Character { get; } // EF 02 2F 00
        private short Unknown1 { get; } // 65 00
        private byte Unknown2 { get; } // 00
        private byte Unknown3 { get; } // 01 - Channel???
        public ushort Location { get; } // 13 27
        private short Unknown4 { get; } // 01 00 - Channel???
        private byte Unknown5 { get; } // 01 - Channel???
        public ulong SessionKey { get; } // 76 7C 3E 01 00 00 00 00

        public DistrictEnterRequest(BinaryReader br)
        {
            Account = br.ReadInt32();
            Character = br.ReadInt32();
            Unknown1 = br.ReadInt16();
            Unknown2 = br.ReadByte();
            Unknown3 = br.ReadByte();
            Location = br.ReadUInt16();
            Unknown4 = br.ReadInt16();
            Unknown5 = br.ReadByte();
            SessionKey = br.ReadUInt64();
        }
    }
}