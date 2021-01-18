using SoulCore.Database.Storages;
using SoulCore.Game.Datas.Bin.Table.Entities;

namespace ow.Service.District.Game.Storage
{
    //public sealed class ItemBrooches
    //{
    //    private byte[] _slots = new byte[15];

    //    public static implicit operator byte[](ItemBrooches o) => o._slots;
    //}

    public sealed class ItemUpgrade
    {
        public static ItemUpgrade Empty => _empty;
        private static readonly ItemUpgrade _empty = new();

        public byte UsedAttempts { get; set; }
        public byte CurrentLevel { get; set; }

        public ItemUpgrade(UpgradeModel model) => (UsedAttempts, CurrentLevel) = (model.UsedAttempts, model.CurrentLevel);

        private ItemUpgrade()
        {
        }
    }

    public sealed class StorageItem
    {
        public static StorageItem Empty => _empty;
        public static readonly StorageItem _empty = new();

        public int Id { get; }
        public ushort Count { get; set; }
        public uint Color { get; set; }
        public ItemTableEntity Prototype { get; }
        public ItemUpgrade Upgrade { get; set; }
        //public ItemBrooches Brooches { get; } = new();

        public StorageItem(ItemModel model, ItemTableEntity prototype)
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
