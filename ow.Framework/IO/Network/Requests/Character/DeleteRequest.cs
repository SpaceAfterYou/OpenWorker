using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct DeleteRequest
    {
        public uint Id { get; }

        public DeleteRequest(BinaryReader br) =>
            Id = br.ReadUInt32();
    }
}
