using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct CharacterListRequest
    {
        public ulong SessionKey { get; }

        public CharacterListRequest(BinaryReader br) => SessionKey = br.ReadUInt64();
    }
}
