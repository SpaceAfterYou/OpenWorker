using System;

namespace ow.Framework.Game.Datas.World.Table.Types
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
