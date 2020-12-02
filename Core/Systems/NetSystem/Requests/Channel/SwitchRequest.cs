using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Channel
{
    [Request]
    public readonly struct SwitchRequest
    {
        public ushort Id { get; }

        public SwitchRequest(BinaryReader br) =>
            Id = br.ReadUInt16();
    }
}
