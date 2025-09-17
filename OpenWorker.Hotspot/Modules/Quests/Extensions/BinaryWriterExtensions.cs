using OpenWorker.Hotspot.Modules.Quests.Enums;

namespace OpenWorker.Hotspot.Modules.Quests.Extensions;

internal static class BinaryWriterExtensions
{
    internal static void Write(this BinaryWriter writer, QuestConditionType value)
    {
        writer.Write((byte)value);
    }
}