using LoginService.Game;
using ow.Framework.IO.Network;

namespace LoginService.Game.Extensions
{
    public static class PacketWriterExtension
    {
        public static void Write(this PacketWriter writer, Option value) => writer.Write((ushort)value);

        public static void Write(this PacketWriter writer, TableMessageId value) => writer.Write((uint)value);
    }
}
