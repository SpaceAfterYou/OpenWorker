using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct ChannelSwitchRequest
    {
        public ushort Id { get; }

        public ChannelSwitchRequest(BinaryReader br) => Id = br.ReadUInt16();
    }
}
