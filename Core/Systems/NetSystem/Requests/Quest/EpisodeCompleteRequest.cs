using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Quest
{
    [Request]
    public readonly struct EpisodeCompleteRequest
    {
        public uint QuestId { get; }
        public uint Episode { get; }

        public EpisodeCompleteRequest(BinaryReader br) =>
            (QuestId, Episode) = (br.ReadUInt32(), br.ReadUInt32());
    }
}
