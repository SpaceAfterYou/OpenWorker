using Core.Systems.NetSystem.Packets;

namespace LoginService.Systems.GameSystem.Extensions
{
    public static class WriterPacketExtension
    {
        public static void Write(this WriterPacket writer, Option value) => writer.Write((ushort)value);

        public static void Write(this WriterPacket writer, TableMessageId value) => writer.Write((uint)value);
    }
}