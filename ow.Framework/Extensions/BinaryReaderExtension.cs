using ow.Framework.Game.Ids;
using ow.Framework.Game.Types;
using System.IO;
using System.Numerics;
using System.Text;

namespace ow.Framework.Extensions
{
    public static class BinaryReaderExtension
    {
        public static string ReadNumberLengthUnicodeString(this BinaryReader br) =>
            Encoding.Unicode.GetString(br.ReadBytes(br.ReadUInt16()));

        public static string ReadByteLengthUnicodeString(this BinaryReader br) =>
            Encoding.Unicode.GetString(br.ReadBytes(br.ReadUInt16() * 2));

        public static string ReadNumberLengthUtf8String(this BinaryReader br)
        {
            ushort length = br.ReadUInt16();
            byte[] rawString = br.ReadBytes(length);
            return Encoding.UTF8.GetString(rawString);
        }

        public static Vector3 ReadVector3(this BinaryReader br) =>
            new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        public static Vector2 ReadVector2(this BinaryReader br) =>
            new(br.ReadSingle(), br.ReadSingle());

        public static HeroId ReadHeroId(this BinaryReader br) =>
            (HeroId)br.ReadByte();

        public static ChatType ReadChatType(this BinaryReader br) =>
            (ChatType)br.ReadByte();

        public static LogoutWayType ReadLogoutWayType(this BinaryReader br) =>
            (LogoutWayType)br.ReadByte();

        public static StorageType ReadStorageType(this BinaryReader br) =>
            (StorageType)br.ReadByte();

        public static GroupRoleType ReadGroupRoleType(this BinaryReader br) =>
            (GroupRoleType)br.ReadByte();

        public static EnterGateWayType ReadEnterGateWayType(this BinaryReader br) =>
            (EnterGateWayType)br.ReadByte();
    }
}