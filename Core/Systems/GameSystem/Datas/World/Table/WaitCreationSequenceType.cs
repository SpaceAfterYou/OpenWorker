using System;

namespace Core.Systems.GameSystem.Datas.World.Table
{
    [Flags]
    public enum WaitCreationSequenceType
    {
        All,
        OnebyOne,
        OnlyOne
    }
}
