using System;

namespace Core.Systems.GameSystem.Datas.World.Table.Types
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
