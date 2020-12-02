using System;

namespace Core.Systems.NetSystem.Permissions
{
    [Flags]
    public enum HandlerPermission
    {
        UnAuthorized = 0x0,
        Authorized = 0x1
    }
}
