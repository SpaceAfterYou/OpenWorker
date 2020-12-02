using System;

namespace Core.Systems.NetSystem.Attributes
{
    [Flags]
    public enum Permission
    {
        UnAuthorized = 0x0,
        Authorized = 0x1
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAttribute : Attribute
    {
        public Permission Permission { init; get; }

        public PermissionAttribute() { }
        public PermissionAttribute(Permission permission) => Permission = permission;
    }
}
