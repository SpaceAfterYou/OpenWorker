using ow.Framework.IO.Lan.Attrubutes;
using ow.Framework.IO.Lan.Requests;
using ow.Service.Auth.Game.Repositories;

namespace ow.Service.Auth.Lan.Handlers
{
    public static class GateHandler
    {
        [Handler("GateAddPlayer")]
        public static void AddPlayer(UpdatePlayerCountRequest request, GateRepository gates) =>
            ++gates[request.GateId].PlayersOnlineCount;

        [Handler("GateAddPlayer")]
        public static void RemovePlayer(UpdatePlayerCountRequest request, GateRepository gates) =>
            --gates[request.GateId].PlayersOnlineCount;
    }
}