using System.IO;
using ow.Framework.IO.Network.Attributes;

namespace ow.Framework.IO.Network.Requests.Movement
{
    [Request]
    public readonly struct LoopMotionEndBtRequest // 0532
    {
        public int CharacterId { get; }

        public LoopMotionEndBtRequest(BinaryReader br) => (CharacterId) = (br.ReadInt32());
    }
}
