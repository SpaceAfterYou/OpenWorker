using System;

namespace Core.Systems.GameSystem.Datas.World.Table
{
    [Flags]
    public enum CreationConditionType
    {
        Disable,
        Immediate,
        WaitSignal
    }
}
