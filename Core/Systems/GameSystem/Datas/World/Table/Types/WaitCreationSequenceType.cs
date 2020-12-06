using System;

namespace Core.Systems.GameSystem.Datas.World.Table.Types
{
    [Flags]
    public enum WaitCreationSequenceType
    {
        All,
        OnebyOne,
        OnlyOne
    }
}
