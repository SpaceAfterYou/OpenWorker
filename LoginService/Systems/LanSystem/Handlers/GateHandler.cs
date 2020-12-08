using Core.Systems.LanSystem.Attrubutes;
using Core.Systems.LanSystem.Requests;
using LoginService.Systems.GameSystem;

namespace LoginService.Systems.LanSystem.Handlers
{
    public static class GateHandler
    {
        [Handler("GateAddPlayer")]
        public static void AddPlayer(UpdatePlayerCountRequest request, Gates gates)
        {
            ++gates[request.GateId].PlayersOnlineCount;
        }

        [Handler("GateAddPlayer")]
        public static void RemovePlayer(UpdatePlayerCountRequest request, Gates gates)
        {
            --gates[request.GateId].PlayersOnlineCount;
        }
    }
}
