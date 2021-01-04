using System;

namespace ow.Framework.Game.Enums
{
    [Flags]
    public enum ItemClassifyInventoryType : byte
    {
        Generic = 0,
        Fashion = 2,
        Home = 9,
        Extra = 13,
        WatafakIsIt = 19
    }
}