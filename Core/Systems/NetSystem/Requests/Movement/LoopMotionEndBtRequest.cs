using System.IO;
using Core.Systems.NetSystem.Attributes;

namespace Core.Systems.NetSystem.Requests.Movement
{
    [Request]
    public readonly struct LoopMotionEndBtRequest // 0532
    {
        public int CharacterId { get; }

        public LoopMotionEndBtRequest(BinaryReader br) => (CharacterId) = (br.ReadInt32());
    }
}
