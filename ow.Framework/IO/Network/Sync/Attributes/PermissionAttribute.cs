using System;

namespace ow.Framework.IO.Network.Sync.Attributes
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