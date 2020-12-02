using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using System;

namespace Core.Systems.NetSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HandlerAttribute : Attribute
    {
        public HandlerPermission Permission { set; get; }
        public HandlerOpcode Opcode { set; get; }

        public HandlerAttribute(HandlerOpcode opcode, HandlerPermission permission) =>
            (Opcode, Permission) = (opcode, permission);
    }
}
