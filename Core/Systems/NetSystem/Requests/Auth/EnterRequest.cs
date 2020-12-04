using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Extensions;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Auth
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