using Core.Systems.NetSystem.Attributes;
using System.IO;

namespace Core.Systems.NetSystem.Requests
{
    [Request]
    public readonly struct ConnectRequest
    {
        public ushort GateId { get; }

        public ConnectRequest(BinaryReader br) =>
            GateId = br.ReadUInt16();
    }
}
