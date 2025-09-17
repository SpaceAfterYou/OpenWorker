using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Chat.Types;

namespace OpenWorker.Hotspot.Modules.Chat.Extensions;

internal static class BinaryWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, ChatMemoryStatisticsValue value)
    {
        writer.Write(value.Count);
        writer.Write(value.Size);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, ChatMessageAppearance value)
    {
        writer.Write((int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, ChatNotifyType value)
    {
        writer.Write((int)value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteChatMessage(this BinaryWriter writer, string value)
    {
        Debug.Assert(value.Length <= ChatModuleDefines.MaxMessageLength);
        writer.WriteUtf16UnicodeString(value, ChatModuleDefines.MaxMessageLength);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteChatColorMessage(this BinaryWriter writer, string value)
    {
        Debug.Assert(value.Length <= ChatModuleDefines.ColorLength);
        writer.WriteUtf16UnicodeString(value, ChatModuleDefines.ColorLength);
    }
}