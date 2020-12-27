using ow.Framework.IO.Network.Sync.Opcodes;

namespace ow.Framework.Utils
{
    public static class ConvertUtils
    {
        public static ushort LeToBeUInt16(ushort value) =>
            (ushort)((ushort)((value & 0xff) << 8) | value >> 8 & 0xff);

        public static ushort LeToBeUInt16(ServerOpcode value) =>
            (ushort)((ushort)(((ushort)value & 0xff) << 8) | (ushort)value >> 8 & 0xff);

        public static ushort LeToBeUInt16(ClientOpcode value) =>
            (ushort)((ushort)(((ushort)value & 0xff) << 8) | (ushort)value >> 8 & 0xff);

        public static ushort MakeWord(byte a, byte b) =>
            ((ushort)(((byte)(a)) | ((ushort)((byte)(b))) << 8));
    }
}