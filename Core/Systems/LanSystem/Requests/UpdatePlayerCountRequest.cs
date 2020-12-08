using System.IO;

namespace Core.Systems.LanSystem.Requests
{
    public readonly struct UpdatePlayerCountRequest
    {
        public ushort GateId { get; }

        public UpdatePlayerCountRequest(BinaryReader br) =>
            GateId = br.ReadUInt16();
    }
}
