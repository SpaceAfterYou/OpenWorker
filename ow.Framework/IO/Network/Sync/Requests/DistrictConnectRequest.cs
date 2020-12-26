using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct DistrictConnectRequest
    {
        public int CharacterId { get; }
        private byte[] Unknown { get; }

        public DistrictConnectRequest(BinaryReader br)
        {
            CharacterId = br.ReadInt32();
            Unknown = br.ReadBytes(13);
        }
    }
}
