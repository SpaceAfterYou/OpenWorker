using ow.Framework.Game.Enums;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ow.Framework.Extensions
{
    public static class BinaryReaderExtension
    {
        public static uint[] ReadUInt32Array(this BinaryReader br, byte count) =>
            Enumerable.Repeat(0, count).Select(_ => br.ReadUInt32()).ToArray();

        public static byte[] ReadByteArray(this BinaryReader br, byte count) =>
            Enumerable.Repeat(0, count).Select(_ => br.ReadByte()).ToArray();

        public static string[] ReadByteLengthUnicodeStringArray(this BinaryReader br, byte count) =>
            Enumerable.Repeat(0, count).Select(_ => br.ReadByteLengthUnicodeString()).ToArray();

        public static string ReadNumberLengthUnicodeString(this BinaryReader br) =>
            Encoding.Unicode.GetString(br.ReadBytes(br.ReadUInt16()));

        public static string ReadByteLengthUnicodeString(this BinaryReader br) =>
            Encoding.Unicode.GetString(br.ReadBytes(br.ReadUInt16() * 2));

        public static string ReadNumberLengthUtf8String(this BinaryReader br) =>
            Encoding.UTF8.GetString(br.ReadBytes(br.ReadUInt16()));

        public static Vector3 ReadVector3(this BinaryReader br) =>
            new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        public static Vector2 ReadVector2(this BinaryReader br) =>
            new(br.ReadSingle(), br.ReadSingle());

        public static Hero ReadHero(this BinaryReader br) =>
            (Hero)br.ReadByte();

        public static ChatType ReadChatType(this BinaryReader br) =>
            (ChatType)br.ReadByte();

        public static LogoutWay ReadLogoutWayType(this BinaryReader br) =>
            (LogoutWay)br.ReadByte();

        public static StorageType ReadStorageType(this BinaryReader br) =>
            (StorageType)br.ReadByte();

        public static GroupRole ReadGroupRoleType(this BinaryReader br) =>
            (GroupRole)br.ReadByte();

        public static EnterGateWay ReadEnterGateWayType(this BinaryReader br) =>
            (EnterGateWay)br.ReadByte();

        public static ItemClassifyType ReadItemClassifyType(this BinaryReader br) =>
            (ItemClassifyType)br.ReadByte();
    }
}