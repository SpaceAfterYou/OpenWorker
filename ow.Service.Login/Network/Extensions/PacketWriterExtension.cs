using ow.Service.Login.Game;
using ow.Framework.IO.Network;

namespace ow.Service.Login.Network.Extensions
{
    internal static class PacketWriterExtension
    {
        internal static void WriteLoginResponseType(this PacketWriter writer, ResponseType value) =>
            writer.Write((byte)value);
    }
}
