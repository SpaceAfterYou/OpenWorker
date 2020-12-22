using ow.Framework.Database.Storages;
using ow.Framework.Game.Datas.Bin.Table.Entities;

namespace ow.Service.District.Game.Storage
{
    //internal sealed class ItemBrooches
    //{
    //    private byte[] _slots = new byte[15];

    //    public static implicit operator byte[](ItemBrooches o) => o._slots;
    //}

    internal sealed class ItemUpgrade
    {
        internal static ItemUpgrade Empty => _empty;
        private static readonly ItemUpgrade _empty = new();

        public byte UsedAttempts { get; set; }
        public byte CurrentLevel { get; set; }

        internal ItemUpgrade(UpgradeModel model) => (UsedAttempts, CurrentLevel) = (model.UsedAttempts, model.CurrentLevel);

        private ItemUpgrade()
        {
        }
    }

    internal sealed class StorageItem
    {
        internal static StorageItem Empty => _empty;
        internal static readonly StorageItem _empty = new();

        internal int Id { get; }
        internal ushort Count { get; set; }
        internal uint Color { get; set; }
        internal ItemTableEntity Prototype { get; }
        internal ItemUpgrade Upgrade { get; set; }
        //internal ItemBrooches Brooches { get; } = new();

        internal StorageItem(ItemModel model, ItemTableEntity prototype)
        {
            Id = model.Id;
            Count = model.Count;
            Prototype = prototype;
            Upgrade = new(model.Upgrade);
        }

        private StorageItem()
        {
            Prototype = ItemTableEntity.Empty;
            Upgrade = ItemUpgrade.Empty;
        }
    }
}