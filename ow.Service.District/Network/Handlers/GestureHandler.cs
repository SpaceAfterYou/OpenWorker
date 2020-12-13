using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Gesture;
using ow.Service.District.Game;

namespace ow.Service.District.Network.Handlers
{
    internal static class GestureHandler
    {
        [Handler(ServerOpcode.GestureDo, HandlerPermission.Authorized)]
        public static void GetOthers(GameSession session, DoRequest request) => session.Entity.Get<Dimension>()
            .BroadcastGestureDo(session, request);
    }
}