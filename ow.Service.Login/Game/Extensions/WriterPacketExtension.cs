using ow.Service.Login.Game;
using ow.Framework.IO.Network;

namespace ow.Service.Login.Game.Extensions
{
    public static class PacketWriterExtension
    {
        public static void Write(this PacketWriter writer, Option value) => writer.Write((ushort)value);

        public static void Write(this PacketWriter writer, TableMessageId value) => writer.Write((uint)value);
    }
}
