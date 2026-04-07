using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Quests.Enums;

namespace OpenWorker.Hotspot.Modules.Quests.Extensions;

internal static class BinaryReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static QuestHelperType ReadQuestHelperType(this BinaryReader reader) => (QuestHelperType)reader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static QuestConditionType ReadQuestConditionType(this BinaryReader reader) => (QuestConditionType)reader.ReadByte();
}
