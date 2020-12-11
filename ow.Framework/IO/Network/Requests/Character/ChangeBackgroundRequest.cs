using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct ChangeBackgroundRequest
    {
        public int AccountId { get; }
        public uint BackgroundId { get; }
        private readonly uint _1;

        public ChangeBackgroundRequest(BinaryReader br)
        {
            AccountId = br.ReadByte();
            BackgroundId = br.ReadByte();
            _1 = br.ReadUInt32();
        }
    }
}