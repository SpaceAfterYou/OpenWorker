using System;

namespace Core.Systems.GameSystem.Datas.World.Table
{
    [Flags]
    public enum PointType
    {
        None,
        StartReturnWait,
        StartReturnGo,
        Wait,
        Return,
        Delete
    }
}
