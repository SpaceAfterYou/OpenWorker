using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct DistructConnectRequest
    {
        public int CharacterId { get; }
        private byte[] Unknown { get; }

        public DistructConnectRequest(BinaryReader br)
        {
            CharacterId = br.ReadInt32();
            Unknown = br.ReadBytes(13);
        }
    }
}
