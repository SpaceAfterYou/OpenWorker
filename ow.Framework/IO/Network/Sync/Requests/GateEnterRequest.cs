using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
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
