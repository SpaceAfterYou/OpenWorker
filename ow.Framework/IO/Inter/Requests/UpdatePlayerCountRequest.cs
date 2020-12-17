using System.IO;

namespace ow.Framework.IO.Lan.Requests
{
    public readonly struct UpdatePlayerCountRequest
    {
        public ushort GateId { get; }

        public UpdatePlayerCountRequest(BinaryReader br) =>
            GateId = br.ReadUInt16();
    }
}
