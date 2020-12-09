using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Channel
{
    [Request]
    public readonly struct SwitchRequest
    {
        public ushort Id { get; }

        public SwitchRequest(BinaryReader br) =>
            Id = br.ReadUInt16();
    }
}
