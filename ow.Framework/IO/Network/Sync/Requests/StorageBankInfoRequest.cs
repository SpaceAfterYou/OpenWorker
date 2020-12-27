using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct StorageBankInfoRequest
    {
        public byte Unknown1 { get; }

        public StorageBankInfoRequest(BinaryReader br) => Unknown1 = br.ReadByte();
    }
}
