using ow.Framework.Database.Storages;

namespace ow.Service.Gate.Game
{
    public sealed class Item
    {
        public int PrototypeId { get; init; }
        public byte UpgradeLevel { get; init; }
        public uint Color { get; init; }

        public Item(ItemModel model)
        {
            PrototypeId = model.PrototypeId;
            UpgradeLevel = model.Upgrade.CurrentLevel;
            Color = model.Color;
        }
    }
}