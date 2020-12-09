using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct SelectRequest
    {
        public int Id { get; }
        private byte[] Unknown { get; }

        public SelectRequest(BinaryReader br)
        {
            Id = br.ReadInt32();
            Unknown = br.ReadBytes(13);
        }
    }
}
