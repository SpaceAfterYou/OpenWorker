using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Extensions;

internal static class BinaryWriterExtensions
{
    private static void Write(this BinaryWriter writer, DailyMissionType value)
    {
        writer.Write((byte)value);
    }

    internal static void Write(this BinaryWriter writer, DailyMissionState value)
    {
        writer.Write((byte)value);
    }

    internal static void Write(this BinaryWriter writer, DailyMissionHelperState value)
    {
        writer.Write((byte)value);
    }

    internal static void Write(this BinaryWriter writer, IReadOnlyList<DailyMissionValue> list)
    {
        writer.Write(1); // group's count

        // foreach group
        // {
        writer.Write((short)1); // count in group

        var now = DateTimeOffset.UtcNow;

        writer.Write((short)list.Count);

        foreach (var value in list)
        {
            writer.Write(value.Mission);
            writer.Write(value.Type);
            writer.Write(value.State);
            writer.Write(value.Helper);
            writer.Write(value.Value);
            writer.Write(value.Accept);
            writer.Write(value.Start);
            writer.Write(value.End);

            var remainTime = value.End - now;
            var durationTime = value.End - value.Start;

            writer.Write(remainTime > TimeSpan.Zero ? remainTime : TimeSpan.Zero);
            writer.Write(durationTime > TimeSpan.Zero ? durationTime : TimeSpan.Zero);
        }
        // }
    }
}