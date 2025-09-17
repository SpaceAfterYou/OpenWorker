using OpenWorker.Hotspot.Modules.DailyMissions.Enums;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Extensions;

internal static class BinaryReaderHelper
{
    internal static DailyMissionType ReadDailyMissionType(this BinaryReader reader)
    {
        return (DailyMissionType)reader.ReadByte();
    }
}