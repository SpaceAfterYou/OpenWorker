using ow.Framework.Game;
using ow.Framework.Game.Ids;
using ow.Framework.Game.Statuses;
using ow.Framework.Game.Types;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.Utils;
using System.IO;
using System.Numerics;
using System.Text;

namespace ow.Framework.IO.Network
{
    public sealed class PacketWriter : BinaryWriter
    {
        public void WriteNumberLengthUtf8String(string str)
        {
            Write((ushort)str.Length);
            Write(Encoding.UTF8.GetBytes(str));
        }

        public void WriteByteLengthUtf8String(string str)
        {
            Write((ushort)(str.Length * 2));
            Write(Encoding.UTF8.GetBytes(str));
        }

        public void WriteNumberLengthChars(char[] str)
        {
            Write((ushort)str.Length);
            Write(str);
        }

        public void WriteNumberLengthUnicodeString(string str)
        {
            Write((ushort)str.Length);
            Write(Encoding.Unicode.GetBytes(str));
        }

        public void WriteByteLengthUnicodeString(string str)
        {
            Write((ushort)(str.Length * 2));
            Write(Encoding.Unicode.GetBytes(str));
        }

        public void WriteVector2(in Vector2 value)
        {
            Write(value.X);
            Write(value.Y);
        }

        public void WriteNpcVisability(NpcVisablity value) =>
            Write((byte)value);

        public void WriteVector3(in Vector3 value)
        {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
        }

        public void WriteChatType(ChatType value) => Write((uint)value);

        public void WriteHeroId(HeroId value) => Write((byte)value);

        public void WriteGateStatus(GateStatus value) => Write((byte)value);

        public PacketWriter(ClientOpcode opcode) : base(new MemoryStream(ushort.MaxValue), Encoding.ASCII, false)
        {
            /// Write SoulWorker magic bytes
            Write((byte)0x02);
            Write((byte)0x00);

            /// Write packet size (just reserve space, overwritten in GetBuffer)
            Write(ushort.MaxValue);

            /// I dont know
            Write((byte)0x1);

            /// Write packet opcode
            Write(ConvertUtils.LeToBeUInt16(opcode));
        }

        internal byte[] GetBuffer()
        {
            /// Skip SoulWorker magic bytes
            Seek(sizeof(ushort), SeekOrigin.Begin);

            /// Write actual packet size
            Write((ushort)BaseStream.Length);

            /// Get and return RAW buffer
            return ((MemoryStream)BaseStream).GetBuffer();
        }
    }
}