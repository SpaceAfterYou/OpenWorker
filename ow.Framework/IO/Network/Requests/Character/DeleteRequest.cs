using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct DeleteRequest
    {
        public int Id { get; }

        public DeleteRequest(BinaryReader br) => Id = br.ReadInt32();
    }
}