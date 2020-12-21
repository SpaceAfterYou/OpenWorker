using ow.Framework.Extensions;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct AuthEnterRequest
    {
        public string Nickname { get; }
        public string Password { get; }
        public string Mac { get; }
        public uint Version { get; }

        public AuthEnterRequest(BinaryReader br)
        {
            Nickname = br.ReadNumberLengthUnicodeString();
            Password = br.ReadNumberLengthUnicodeString();
            Mac = br.ReadNumberLengthUnicodeString();
            Version = br.ReadUInt32();
        }
    }
}
