using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Persons.Extensions;

internal static class BinaryWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, Hero value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WritePersonName(this BinaryWriter writer, string value)
    {
        Debug.Assert(value.Length <= PersonModuleDefines.MaxNameLength);
        writer.WriteUtf16UnicodeString(value, PersonModuleDefines.MaxNameLength);
    }
}
