using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Storage
{
    [Request]
    public readonly struct BankInfoRequest
    {
        public byte Unknown1 { get; }

        public BankInfoRequest(BinaryReader br) =>
            Unknown1 = br.ReadByte();
    }
}
