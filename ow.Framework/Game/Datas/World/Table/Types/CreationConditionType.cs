using System;

namespace ow.Framework.Game.Datas.World.Table.Types
{
    [Flags]
    public enum CreationConditionType
    {
        Disable,
        Immediate,
        WaitSignal
    }
}
