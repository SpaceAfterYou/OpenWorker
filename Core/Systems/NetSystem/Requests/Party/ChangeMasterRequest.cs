using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Party
{
    [Request]
    public readonly struct ChangeMasterRequest
    {
        public int Id { get; }

        public ChangeMasterRequest(BinaryReader br)
            => Id = br.ReadInt32();
    }
}
