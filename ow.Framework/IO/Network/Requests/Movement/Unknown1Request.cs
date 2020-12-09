using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Movement
{
    [Request]
    public readonly struct Unknown1Request
    {
        public int CharacterId { get; }
        public uint Unknown1 { get; }

        public Unknown1Request(BinaryReader br)
            => (CharacterId, Unknown1) = (br.ReadInt32(), br.ReadUInt32());
    }
}
