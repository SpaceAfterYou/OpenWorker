using LoginService.Game;
using ow.Framework.IO.Network;

namespace LoginService.Network.Extensions
{
    internal static class PacketWriterExtension
    {
        internal static void WriteLoginResponseType(this PacketWriter writer, ResponseType value) =>
            writer.Write((byte)value);
    }
}