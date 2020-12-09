using ow.Framework.IO.Network.Attributes;
using ow.Framework.Extensions;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Party
{
    [Request]
    public readonly struct InviteRequest
    {
        public string CharacterName { get; }
        public short Unknown1 { get; } // location id???
        public int CharacterId { get; }
        public int Unknown2 { get; }
        public int Unknown3 { get; }
        public int Unknown4 { get; }
        public byte Unknown5 { get; }

        public InviteRequest(BinaryReader br)
        {
            CharacterName = br.ReadNumberLengthUnicodeString();
            Unknown1 = br.ReadInt16();
            CharacterId = br.ReadInt32();
            Unknown2 = br.ReadInt32();
            Unknown3 = br.ReadInt32();
            Unknown4 = br.ReadInt32();
            Unknown5 = br.ReadByte();
        }
    }
}
