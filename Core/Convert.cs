using Core.Systems.NetSystem.Opcodes;

namespace Core
{
    public static class Convert
    {
        public static ushort LeToBeUInt16(ushort value) =>
            (ushort)((ushort)((value & 0xff) << 8) | value >> 8 & 0xff);

        public static ushort LeToBeUInt16(HandlerOpcode value) =>
            (ushort)((ushort)(((ushort)value & 0xff) << 8) | (ushort)value >> 8 & 0xff);
    }
}