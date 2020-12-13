using ow.Framework.Database.Storages;
using ow.Framework.Game.Storage.Item;
using System;

namespace ow.Service.Gate.Game
{
    public sealed class Item : IItemStorage
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