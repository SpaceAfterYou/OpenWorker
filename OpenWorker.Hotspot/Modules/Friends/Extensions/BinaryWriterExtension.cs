using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Friends.Types;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Friends.Extensions;

internal static class BinaryWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, FriendBlockValue value)
    {
        writer.Write(value.Person);

        Debug.Assert(value.Name.Length <= 21);
        writer.WriteUtf16UnicodeString(value.Name);

        writer.Write(value.Level);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, FriendResult value)
    {
        writer.Write((int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, FriendState value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, FriendValue value)
    {
        Debug.Assert(value.Name.Length <= 21);
        writer.WriteUtf16UnicodeString(value.Name);

        writer.Write(value.Friend);
        writer.Write(value.Level);
        writer.Write(value.Class);
        writer.Write(value.State);
        writer.Write(value.CommunityState);

        Debug.Assert(value.Name.Length <= FriendModuleDefines.MaxNoteLength);
        writer.WriteUtf16UnicodeString(value.Note);

        writer.Write(value.Channel);
        writer.Write(value.World);
        writer.Write(value.FriendPoint);
        writer.Write(value.Login);
        writer.Write(value.LogOut);
        writer.Write(value.Remain);
    }
}