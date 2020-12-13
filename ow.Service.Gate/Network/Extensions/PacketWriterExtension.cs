using ow.Framework.IO.Network;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Enums;

namespace ow.Service.Gate.Network.Extensions
{
    internal static class PacketWriterExtension
    {
        internal static void Write(this PacketWriter writer, GateEnterResult value) =>
            writer.Write((byte)value);

        internal static void Write(this PacketWriter writer, Place value)
        {
            writer.Write(value.Location);
            writer.Write((ulong)0);
            writer.WriteVector3(value.Position);
            writer.Write(value.Rotation);
            writer.Write((float)0); // ??? armour gage ???
            writer.Write((float)0); // ??? armour gage ???
        }
    }
}