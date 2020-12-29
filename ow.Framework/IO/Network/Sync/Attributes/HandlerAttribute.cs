using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using System;

namespace ow.Framework.IO.Network.Sync.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HandlerAttribute : Attribute
    {
        public HandlerPermission Permission { get; }
        public ServerOpcode Opcode { get; }

        public HandlerAttribute(ServerOpcode opcode, HandlerPermission permission) =>
            (Opcode, Permission) = (opcode, permission);
    }
}