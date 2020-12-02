using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Character
{
    [Request]
    public readonly struct DeleteRequest
    {
        public uint Id { get; }

        public DeleteRequest(BinaryReader br) =>
            Id = br.ReadUInt32();
    }
}
