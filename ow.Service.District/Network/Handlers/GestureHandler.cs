using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Gesture;

namespace ow.Service.District.Network.Handlers
{
    internal static class GestureHandler
    {
        [Handler(ServerOpcode.GestureDo, HandlerPermission.Authorized)]
        public static void GetOthers(Session session, DoRequest request) => session
            .Channel?.BroadcastGestureDo(session, request);
    }
}