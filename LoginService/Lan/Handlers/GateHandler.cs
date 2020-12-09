using LoginService.Game;
using ow.Framework.IO.Lan.Attrubutes;
using ow.Framework.IO.Lan.Requests;

namespace LoginService.Lan.Handlers
{
    public static class GateHandler
    {
        [Handler("GateAddPlayer")]
        public static void AddPlayer(UpdatePlayerCountRequest request, Gates gates) =>
            ++gates[request.GateId].PlayersOnlineCount;

        [Handler("GateAddPlayer")]
        public static void RemovePlayer(UpdatePlayerCountRequest request, Gates gates) =>
            --gates[request.GateId].PlayersOnlineCount;
    }
}