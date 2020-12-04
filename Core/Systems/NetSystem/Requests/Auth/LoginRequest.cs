using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Extensions;
using System.IO;

namespace Core.Systems.NetSystem.Requests
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