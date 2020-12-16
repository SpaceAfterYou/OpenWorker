using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Server
{
    [Request]
    public readonly struct EnterRequest
    {
        public int AccountId { get; } // 6D 38 08 00
        public int CharacterId { get; } // EF 02 2F 00
        private short Unknown1 { get; } // 65 00
        private byte Unknown2 { get; } // 00
        private byte Unknown3 { get; } // 01 - Channel???
        public ushort LocationId { get; } // 13 27
        private short Unknown4 { get; } // 01 00 - Channel???
        private byte Unknown5 { get; } // 01 - Channel???
        public ulong SessionKey { get; } // 76 7C 3E 01 00 00 00 00

        public EnterRequest(BinaryReader br)
        {
            AccountId = br.ReadInt32();
            CharacterId = br.ReadInt32();
            Unknown1 = br.ReadInt16();
            Unknown2 = br.ReadByte();
            Unknown3 = br.ReadByte();
            LocationId = br.ReadUInt16();
            Unknown4 = br.ReadInt16();
            Unknown5 = br.ReadByte();
            SessionKey = br.ReadUInt64();
        }
    }
}