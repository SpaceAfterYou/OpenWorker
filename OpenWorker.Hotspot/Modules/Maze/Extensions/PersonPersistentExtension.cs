using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Modules.Maze.Components;

namespace OpenWorker.Hotspot.Modules.Maze.Extensions;

public static class PersonPersistentExtension
{
    public static FatiguePointsComponent ToFatiguePointsComponent(this PersonPersistent value)
    {
        return new FatiguePointsComponent
        {
            Bonus = value.FatiguePointBonus,
            Common = value.FatiguePointCommon
        };
    }
}