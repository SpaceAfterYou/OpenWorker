using ow.Framework.Extensions;
using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct SAuthEnterRequest
    {
        public string Nickname { get; }
        public string Password { get; }
        public string Mac { get; }
        public uint Version { get; }

        public SAuthEnterRequest(BinaryReader br)
        {
            Nickname = br.ReadNumberLengthUnicodeString();
            Password = br.ReadNumberLengthUnicodeString();
            Mac = br.ReadNumberLengthUnicodeString();
            Version = br.ReadUInt32();
        }
    }
}
