using System.IO;
using ow.Framework.IO.Network.Sync.Attributes;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct QuestEpisodeCompleteRequest
    {
        public uint QuestId { get; }
        public uint Episode { get; }

        public QuestEpisodeCompleteRequest(BinaryReader br) => (QuestId, Episode) = (br.ReadUInt32(), br.ReadUInt32());
    }
}
