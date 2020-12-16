using ow.Framework.IO.Network;
using ow.Service.Gate.Game.Enums;

namespace ow.Service.Gate.Network.Extensions
{
    internal static class PacketWriterExtension
    {
        internal static void Write(this PacketWriter writer, GateEnterResult value) => writer.Write((byte)value);
    }
}