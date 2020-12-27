using System;

namespace ow.Framework.IO.Network.Sync
{
    [Flags]
    public enum Permission
    {
        UnAuthorized = 0x0,
        Authorized = 0x1
    }
}