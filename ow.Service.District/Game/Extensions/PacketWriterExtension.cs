using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Service.District.Game.Enums;

namespace ow.Service.District.Game
{
    internal static class PacketWriterExtension
    {
        internal static void WriteCanLogOutConnect(this PacketWriter writer, CanLogOutConnect value) =>
            writer.Write((byte)value);

        internal static void WriteLogoutWay(this PacketWriter writer, LogoutWay value) =>
            writer.Write((byte)value);
    }
}