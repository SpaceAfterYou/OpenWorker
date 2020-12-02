using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Character
{
    [Request]
    public readonly struct ListRequest
    {
        public ulong SessionKey { get; }

        public ListRequest(BinaryReader br) =>
            SessionKey = br.ReadUInt64();
    }
}
