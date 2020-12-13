using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Server
{
    [Request]
    public readonly struct LogoutRequest
    {
        public int CharacterId { get; }
        public int AccountId { get; }
        public LogoutWay Way { get; }

        public LogoutRequest(BinaryReader br) =>
            (CharacterId, AccountId, Way) = (br.ReadInt32(), br.ReadInt32(), br.ReadLogoutWayType());
    }
}