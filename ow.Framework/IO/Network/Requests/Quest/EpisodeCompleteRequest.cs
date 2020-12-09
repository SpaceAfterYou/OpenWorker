using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Quest
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
