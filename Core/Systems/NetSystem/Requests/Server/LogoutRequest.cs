using Core.Systems.NetSystem.Attributes;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Server
{
    [Request]
    public readonly struct LogoutRequest
    {
        public int CharacterId { get; }
        public int AccountId { get; }
        public LogoutWayType Way { get; }

        public LogoutRequest(BinaryReader br) =>
            (CharacterId, AccountId, Way) = (br.ReadInt32(), br.ReadInt32(), br.ReadLogoutWayType());
    }
}
