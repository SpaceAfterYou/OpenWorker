using Core.Systems.NetSystem.Opcodes;
using SoulWorker.Types;
using System;
using System.IO;
using System.Text;

namespace Core.Systems.NetSystem.Packets
{
    public class WriterPacket : IDisposable
    {
        public void WriteNumberLengthUtf8String(string str)
        {
            BinaryWriter.Write((ushort)str.Length);
            BinaryWriter.Write(Encoding.UTF8.GetBytes(str));
        }

        public void WriteByteLengthUtf8String(string str)
        {
            BinaryWriter.Write((ushort)(str.Length * 2));
            BinaryWriter.Write(Encoding.UTF8.GetBytes(str));
        }

        public void WriteNumberLengthChars(char[] str)
        {
            BinaryWriter.Write((ushort)str.Length);
            BinaryWriter.Write(str);
        }

        public void WriteNumberLengthUnicodeString(string str)
        {
            BinaryWriter.Write((ushort)(str.Length));
            BinaryWriter.Write(Encoding.Unicode.GetBytes(str));
        }

        public void WriteByteLengthUnicodeString(string str)
        {
            BinaryWriter.Write((ushort)(str.Length * 2));
            BinaryWriter.Write(Encoding.Unicode.GetBytes(str));
        }

        public void Write(GateStatusType value) => BinaryWriter.Write((byte)value);

        public void Write(ResponseType value) => BinaryWriter.Write((byte)value);

        public void Write(ulong value) => BinaryWriter.Write(value);

        public void Write(uint value) => BinaryWriter.Write(value);

        public void Write(ushort value) => BinaryWriter.Write(value);

        public void Write(float value) => BinaryWriter.Write(value);

        public void Write(sbyte value) => BinaryWriter.Write(value);

        public void Write(long value) => BinaryWriter.Write(value);

        public void Write(int value) => BinaryWriter.Write(value);

        public void Write(double value) => BinaryWriter.Write(value);

        public void Write(decimal value) => BinaryWriter.Write(value);

        public void Write(byte[] value) => BinaryWriter.Write(value);

        public void Write(byte value) => BinaryWriter.Write(value);

        public void Write(bool value) => BinaryWriter.Write(value);

        public void Write(short value) => BinaryWriter.Write(value);

        public void Dispose()
        {
            BinaryWriter.Dispose();
            MemoryStream.Dispose();
        }

        public WriterPacket(ClientOpcode client)
        {
            MemoryStream = new MemoryStream(ushort.MaxValue);
            BinaryWriter = new BinaryWriter(MemoryStream);

            Write((byte)0x02);
            Write((byte)0x00);
            Write(ushort.MaxValue);
            Write((byte)0x1);
            Write((ushort)client);
        }

        internal byte[] GetBuffer()
        {
            BinaryWriter.Seek(2, SeekOrigin.Begin);
            BinaryWriter.Write((ushort)BinaryWriter.BaseStream.Length);

            return MemoryStream.GetBuffer();
        }

        internal int Capacity => MemoryStream.Capacity;
        internal long Length => MemoryStream.Length;

        private MemoryStream MemoryStream { get; }
        private BinaryWriter BinaryWriter { get; }
    }
}