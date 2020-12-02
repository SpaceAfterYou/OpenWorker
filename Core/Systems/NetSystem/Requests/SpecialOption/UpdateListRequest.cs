using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.SpecialOption
{
    [Request]
    public readonly struct UpdateListRequest
    {
        public int CharacterId { get; }

        public UpdateListRequest(BinaryReader br) =>
            CharacterId = br.ReadInt32();
    }
}
