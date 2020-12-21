using System;

namespace ow.Framework.Game.Enums
{
    [Flags]
    public enum DistrictLogOutStatus : byte
    {
        Failure = 0,
        Success = 1,
    }
}