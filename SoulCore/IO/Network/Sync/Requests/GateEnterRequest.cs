using SoulCore.Extensions;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct GateEnterRequest
    {
        public int Account { get; }
        public ushort Gate { get; }
        public ulong SessionKey { get; }
        public EnterGateWay Way { get; }

        public GateEnterRequest(BinaryReader br)
        {
            Account = br.ReadInt32();
            Gate = br.ReadUInt16();
            SessionKey = br.ReadUInt64();
            Way = br.ReadEnterGateWayType();
        }
    }
}
