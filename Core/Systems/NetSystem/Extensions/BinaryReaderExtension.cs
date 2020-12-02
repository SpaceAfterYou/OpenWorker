using System.IO;
using System.Text;

namespace Core.Systems.NetSystem.Extensions
{
    public static class BinaryReaderExtension
    {
        public static string ReadNumberLengthUnicodeString(this BinaryReader br) =>
            Encoding.Unicode.GetString(br.ReadBytes(br.ReadUInt16()));

        public static string ReadByteLengthUnicodeString(this BinaryReader br) =>
            Encoding.Unicode.GetString(br.ReadBytes(br.ReadUInt16() * 2));

        public static string ReadNumberLengthUtf8String(this BinaryReader br) =>
            Encoding.UTF8.GetString(br.ReadBytes(br.ReadUInt16()));

        //public static Hero ReadHero(this BinaryReader br) =>
        //    (Hero)br.ReadByte();

        //public static ChatType ReadChatType(this BinaryReader br) =>
        //    (ChatType)br.ReadByte();

        //public static LogoutWay ReadLogoutWay(this BinaryReader br) =>
        //    (LogoutWay)br.ReadByte();

        //public static StorageType ReadStorage(this BinaryReader br) =>
        //    (StorageType)br.ReadByte();

        //public static GroupRole ReadGroupRole(this BinaryReader br) =>
        //    (GroupRole)br.ReadByte();

        //public static EnterGateWay ReadEnterGateWay(this BinaryReader br) =>
        //    (EnterGateWay)br.ReadByte();
    }
}
