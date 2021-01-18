using SoulCore.Game.Enums;

namespace SoulCore.Database.Characters
{
    public sealed class StorageModel
    {
        public StorageType Type { get; set; }
        public byte Upgrades { get; set; }
    }
}
