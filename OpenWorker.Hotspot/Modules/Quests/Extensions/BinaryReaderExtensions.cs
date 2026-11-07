using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Quests.Enums;

using OpenWorker.Hotspot.Modules.Quests.Types;

namespace OpenWorker.Hotspot.Modules.Quests.Extensions;

internal static class BinaryReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static QuestHelperType ReadQuestHelperType(this BinaryReader reader) => (QuestHelperType)reader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CreateItemValue[] ReadCreateItems(this BinaryReader reader) => Enumerable
        .Range(0, reader.ReadByte())
        .Select(_ => new CreateItemValue(reader))
        .ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static QuestConditionType ReadQuestConditionType(this BinaryReader reader) => (QuestConditionType)reader.ReadByte();
}
