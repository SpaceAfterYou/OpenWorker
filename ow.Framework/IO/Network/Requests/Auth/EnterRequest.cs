using ow.Framework.Extensions;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Auth
{
    [Request]
    public readonly struct EnterRequest
    {
        public string Nickname { get; }
        public string Password { get; }
        public string Mac { get; }
        public uint Version { get; }

        public EnterRequest(BinaryReader br)
        {
            Nickname = br.ReadNumberLengthUnicodeString();
            Password = br.ReadNumberLengthUnicodeString();
            Mac = br.ReadNumberLengthUnicodeString();
            Version = br.ReadUInt32();
        }
    }
}
