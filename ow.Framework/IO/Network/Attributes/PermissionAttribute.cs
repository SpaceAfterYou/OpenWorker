using System;

namespace ow.Framework.IO.Network.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAttribute : Attribute
    {
        public Permission Permission { init; get; }

        public PermissionAttribute()
        {
        }

        public PermissionAttribute(Permission permission) => Permission = permission;
    }
}
