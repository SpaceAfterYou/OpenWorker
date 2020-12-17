using System;

namespace ow.Framework.FS.Datas.World.Table.Types
{
    [Flags]
    public enum CreationConditionType
    {
        Disable,
        Immediate,
        WaitSignal
    }
}
