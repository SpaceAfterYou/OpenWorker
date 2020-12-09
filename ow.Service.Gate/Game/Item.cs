using ow.Framework.Database.Storages;

namespace ow.Service.Gate.Game
{
    public sealed class Item
    {
        public uint PrototypeId { get; init; }
        public uint UpgradeLevel { get; init; }
        public uint Color { get; init; }

        public Item(ItemModel model)
        {
            PrototypeId = model.PrototypeId;
            UpgradeLevel = model.Upgrade.CurrentLevel;
            Color = model.Color;
        }
    }
}
