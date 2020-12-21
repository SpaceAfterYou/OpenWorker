using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct QuestAcceptRequest
    {
        public uint Id { get; }

        public QuestAcceptRequest(BinaryReader br) =>
            Id = br.ReadUInt32();
    }
}
