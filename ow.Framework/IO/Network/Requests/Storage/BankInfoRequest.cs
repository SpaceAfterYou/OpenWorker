using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Storage
{
    [Request]
    public readonly struct BankInfoRequest
    {
        public byte Unknown1 { get; }

        public BankInfoRequest(BinaryReader br) =>
            Unknown1 = br.ReadByte();
    }
}
