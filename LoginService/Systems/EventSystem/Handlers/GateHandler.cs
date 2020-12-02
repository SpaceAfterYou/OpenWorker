using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using LoginService.Systems.NetSystem;

namespace LoginService.Systems.EventSystem.Handlers
{
    internal class GateHandler
    {
        [Handler(HandlerOpcode.GateList, HandlerPermission.Authorized)]
        public static void GetList(Session session)
        {
        }

        [Handler(HandlerOpcode.GateConnect, HandlerPermission.Authorized)]
        public static void Connect(Session session)
        {
        }
    }
}