using System.Diagnostics;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Events.Enums;
using OpenWorker.Hotspot.Modules.Events.Types;

namespace OpenWorker.Hotspot.Modules.Events.Extensions;

internal static class BinaryWriterExtension
{
    private static void Write(this BinaryWriter writer, AttendanceState state)
    {
        writer.Write((byte)state);
    }

    private static void Write(this BinaryWriter writer, AttendanceState[] state)
    {
        Debug.Assert(state.Length == 40);
        
        foreach (var item in state)
        {
            writer.Write(item);
        }
    }
    
    internal static void Write(this BinaryWriter writer, AttendancePlayTime time)
    {
        writer.Write(time.Step);
        writer.Write(time.Played);
    }
    
    internal static void Write(this BinaryWriter writer, AttendanceInfo info)
    {
        writer.Write(info.CurrentDay);
        writer.Write(info.State);
        writer.Write(info.Table);
    }
}