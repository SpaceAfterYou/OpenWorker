using SoulWorker.Types;
using System.IO;

namespace SoulWorker.Extensions
{
    public static class BinaryReaderExtension
    {
        public static HeroType ReadHeroType(this BinaryReader br) =>
            (HeroType)br.ReadByte();

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