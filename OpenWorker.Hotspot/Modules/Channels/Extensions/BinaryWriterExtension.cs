using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Channels.Types;

namespace OpenWorker.Hotspot.Modules.Channels.Extensions;

internal static class BinaryWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, ChannelWorkload value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, ChannelValue value)
    {
        writer.Write(value.Id);
        writer.Write(value.Workload);
    }
}