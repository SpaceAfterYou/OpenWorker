using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Quest
{
    [Request]
    public readonly struct AcceptRequest
    {
        public uint Id { get; }

        public AcceptRequest(BinaryReader br) =>
            Id = br.ReadUInt32();
    }
}
