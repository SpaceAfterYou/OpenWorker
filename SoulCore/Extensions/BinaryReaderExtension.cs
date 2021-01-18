using SoulCore.Game.Enums;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SoulCore.Extensions
{
    public static class BinaryReaderExtension
    {
        public static uint[] ReadUInt32Array(this BinaryReader br, byte count) =>
            Enumerable.Repeat(0, count).Select(_ => br.ReadUInt32()).ToArray();

        public static int[] ReadInt32Array(this BinaryReader br, byte count) =>
            Enumerable.Repeat(0, count).Select(_ => br.ReadInt32()).ToArray();

        public static ushort[] ReadUInt16Array(this BinaryReader br, byte count) =>
            Enumerable.Repeat(0, count).Select(_ => br.ReadUInt16()).ToArray();

        public static float[] ReadSingleArray(this BinaryReader br, byte count) =>
            Enumerable.Repeat(0, count).Select(_ => br.ReadSingle()).ToArray();

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

        public static DistrictLogOutWay ReadLogoutWayType(this BinaryReader br) =>
            (DistrictLogOutWay)br.ReadByte();

        public static StorageType ReadStorageType(this BinaryReader br) =>
            (StorageType)br.ReadByte();

        public static CharacterAdvancement ReadCharacterAdvancement(this BinaryReader br) =>
            (CharacterAdvancement)br.ReadByte();

        public static GroupRole ReadGroupRoleType(this BinaryReader br) =>
            (GroupRole)br.ReadByte();

        public static EnterGateWay ReadEnterGateWayType(this BinaryReader br) =>
            (EnterGateWay)br.ReadByte();

        public static ItemClassifySlotType ReadItemClassifySlotType(this BinaryReader br) =>
            (ItemClassifySlotType)br.ReadByte();

        public static ItemClassifyInventoryType ReadItemClassifyInventoryType(this BinaryReader br) =>
            (ItemClassifyInventoryType)br.ReadByte();
    }
}
