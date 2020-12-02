using Core.Systems.NetSystem.Attributes;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Character
{
    [Request]
    public readonly struct InfoRequest
    {
        public int CharacterId { get; }
        public byte Unknown1 { get; }

        public InfoRequest(BinaryReader br) => (CharacterId, Unknown1) = (br.ReadInt32(), br.ReadByte());
    }
}
