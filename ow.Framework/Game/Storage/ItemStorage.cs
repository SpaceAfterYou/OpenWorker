using ow.Framework.Database.Storages;

namespace ow.Framework.FS.Storage
{
    public sealed class ItemStorage
    {
        public int Id { get; }
        public int PrototypeId { get; }
        public uint Color { get; }
        public byte UpgradeLevel { get; }

        public ItemStorage(ItemModel model)
        {
            Id = model.Id;
            PrototypeId = model.PrototypeId;
            Color = model.Color;
            UpgradeLevel = model.Upgrade.CurrentLevel;
        }
    }
}