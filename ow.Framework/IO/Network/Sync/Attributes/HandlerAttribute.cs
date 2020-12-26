using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Opcodes;
using System;

namespace ow.Framework.IO.Network.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HandlerAttribute : Attribute
    {
        public HandlerPermission Permission { set; get; }
        public ServerOpcode Opcode { set; get; }

        public HandlerAttribute(ServerOpcode opcode, HandlerPermission permission) =>
            (Opcode, Permission) = (opcode, permission);
    }
}
