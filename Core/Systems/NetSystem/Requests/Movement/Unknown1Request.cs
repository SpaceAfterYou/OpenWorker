using Core.Systems.NetSystem.Attributes;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Movement
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
