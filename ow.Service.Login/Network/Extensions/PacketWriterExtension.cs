using ow.Framework.IO.Network;
using ow.Service.Login.Game;

namespace ow.Service.Login.Network.Extensions
{
    internal static class PacketWriterExtension
    {
        internal static void Write(this PacketWriter writer, TableMessage value) =>
            writer.Write((uint)value);

        internal static void WriteLoginResponseType(this PacketWriter writer, ResponseType value) =>
            writer.Write((byte)value);
    }
}