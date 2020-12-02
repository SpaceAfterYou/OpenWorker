using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Gate
{
    [Request]
    public readonly struct EnterRequest
    {
        public int AccountId { get; }
        public ushort GateId { get; }
        public ulong SessionKey { get; }
        public EnterGateWayType Way { get; }

        public EnterRequest(BinaryReader br)
        {
            AccountId = br.ReadInt32();
            GateId = br.ReadUInt16();
            SessionKey = br.ReadUInt64();
            Way = br.ReadEnterGateWayType();
        }
    }
}
