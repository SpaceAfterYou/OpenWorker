using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests
{
    [Request]
    public readonly struct ChannelSwitchRequest
    {
        public ushort Id { get; }

        public ChannelSwitchRequest(BinaryReader br) => Id = br.ReadUInt16();
    }
}
