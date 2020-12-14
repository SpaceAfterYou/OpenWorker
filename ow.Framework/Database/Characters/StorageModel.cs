using ow.Framework.Game.Enums;

namespace ow.Framework.Database.Characters
{
    public sealed class StorageModel
    {
        public StorageType Type { get; set; }
        public byte Upgrades { get; set; }
    }
}