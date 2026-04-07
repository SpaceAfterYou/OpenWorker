using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Quests.Responses;

namespace OpenWorker.Hotspot.Modules.Quests.Extensions;

internal static class BinaryWriterExtensions
{
    internal static void Write(this BinaryWriter writer, QuestConditionType value)
    {
        writer.Write((byte)value);
    }

    internal static void Write(this BinaryWriter writer, QuestHelperType value)
    {
        writer.Write((byte)value);
    }
}
