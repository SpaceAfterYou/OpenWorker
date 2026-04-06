using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Channels.Types;

namespace OpenWorker.Hotspot.Modules.Channels.Extensions;

internal static class BinaryWriterExtension
{
    extension(BinaryWriter writer)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(ChannelWorkload value)
        {
            writer.Write((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write(ChannelValue value)
        {
            writer.Write(value.Id);
            writer.Write(value.Workload);
        }
    }
}