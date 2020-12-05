using System;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch
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
