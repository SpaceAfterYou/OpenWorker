using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests
{
    [Request]
    public readonly struct Unknown1Request
    {
        public byte Unknown1 { get; }

        public Unknown1Request(BinaryReader br) =>
            Unknown1 = br.ReadByte();
    }
}
