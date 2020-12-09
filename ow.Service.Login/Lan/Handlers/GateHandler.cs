using ow.Service.Login.Game;
using ow.Framework.IO.Lan.Attrubutes;
using ow.Framework.IO.Lan.Requests;

namespace ow.Service.Login.Lan.Handlers
{
    public static class GateHandler
    {
        [Handler("GateAddPlayer")]
        public static void AddPlayer(UpdatePlayerCountRequest request, GatesInstances gates) =>
            ++gates[request.GateId].PlayersOnlineCount;

        [Handler("GateAddPlayer")]
        public static void RemovePlayer(UpdatePlayerCountRequest request, GatesInstances gates) =>
            --gates[request.GateId].PlayersOnlineCount;
    }
}
