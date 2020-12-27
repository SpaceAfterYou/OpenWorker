using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct QuestAcceptRequest
    {
        public uint Id { get; }

        public QuestAcceptRequest(BinaryReader br) =>
            Id = br.ReadUInt32();
    }
}
