using ow.Framework.Extensions;
using ow.Framework.Game.Types;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Gate
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