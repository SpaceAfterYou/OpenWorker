using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Character
{
    [Request]
    public readonly struct ListRequest
    {
        public ulong SessionKey { get; }

        public ListRequest(BinaryReader br) =>
            SessionKey = br.ReadUInt64();
    }
}
