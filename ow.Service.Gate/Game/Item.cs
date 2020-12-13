using ow.Framework.Database.Storages;
using ow.Framework.Game;
using System;

namespace ow.Service.Gate.Game
{
    public sealed class Item : IReadOnlyItem
    {
        public int PrototypeId { get; }
        public byte UpgradeLevel { get; }
        public uint Color { get; }

        public int Id => throw new NotImplementedException();

        public Item(ItemModel model)
        {
            PrototypeId = model.PrototypeId;
            UpgradeLevel = model.Upgrade.CurrentLevel;
            Color = model.Color;
        }
    }
}