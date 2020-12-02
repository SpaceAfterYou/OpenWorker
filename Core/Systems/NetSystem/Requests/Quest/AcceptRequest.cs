using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Quest
{
    [Request]
    public readonly struct AcceptRequest
    {
        public uint Id { get; }

        public AcceptRequest(BinaryReader br) =>
            Id = br.ReadUInt32();
    }
}
