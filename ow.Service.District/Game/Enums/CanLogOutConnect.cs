using System;

namespace ow.Service.District.Game.Enums
{
    [Flags]
    internal enum CanLogOutConnect : byte
    {
        No = 0,
        Yes = 1,
    }
}