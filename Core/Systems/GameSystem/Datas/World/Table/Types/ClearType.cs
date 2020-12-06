using System;

namespace Core.Systems.GameSystem.Datas.World.Table.Types
{
    [Flags]
    public enum ClearType
    {
        Kill,
        Always,
        None,
        Kill_Perpect,
        ModeClear
    }
}
