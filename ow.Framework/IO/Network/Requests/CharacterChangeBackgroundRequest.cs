using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct CharacterChangeBackgroundRequest
    {
        public int AccountId { get; }
        public uint BackgroundId { get; }
        private readonly uint _1;

        public CharacterChangeBackgroundRequest(BinaryReader br)
        {
            AccountId = br.ReadInt32();
            BackgroundId = br.ReadUInt32();
            _1 = br.ReadUInt32();
        }
    }
}
