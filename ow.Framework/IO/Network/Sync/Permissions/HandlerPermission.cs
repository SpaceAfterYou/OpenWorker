using System;

namespace ow.Framework.IO.Network.Sync.Permissions
{
    [Flags]
    public enum HandlerPermission
    {
        UnAuthorized = 0x0,
        Authorized = 0x1
    }
}