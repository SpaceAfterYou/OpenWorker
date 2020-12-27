using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct MovementLoopMotionEndBroadcastRequest
    {
        public int Character { get; }

        public MovementLoopMotionEndBroadcastRequest(BinaryReader br) => Character = br.ReadInt32();
    }
}
