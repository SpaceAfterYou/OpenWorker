using ow.Framework.IO.Network.Attributes;
using ow.Framework.Extensions;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct LoginRequest
    {
        public string Nickname { get; }
        public string Password { get; }
        public string Mac { get; }
        public uint Version { get; }

        public LoginRequest(BinaryReader br)
        {
            Nickname = br.ReadNumberLengthUnicodeString();
            Password = br.ReadNumberLengthUnicodeString();
            Mac = br.ReadNumberLengthUnicodeString();
            Version = br.ReadUInt32();
        }
    }
}
