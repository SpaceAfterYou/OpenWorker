using OpenWorker.Hotspot.Messages.Response.Person.Enums;

namespace OpenWorker.Hotspot.Messages.Response.Person.Extensions;

internal static class BinaryWriterExtension
{
    public static void Write(this BinaryWriter writer, PasswordProtectionState value)
    {
        writer.Write((byte)value);
    }
    //
    // public static void Write(this BinaryWriter writer, E_PASSWORD_CHECK_TYPE value)
    // {
    //     writer.Write((byte)value);
    // }
}