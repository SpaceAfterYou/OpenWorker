using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using ow.Framework.Game.Storage;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Gate.Game
{
    public sealed class Storage : IStorage
    {
        public EquipableBattleFashionStorage EquippedBattleFashionStorage { get; }
        public EquipableViewFashionStorage EquippedViewFashionStorage { get; }
        public EquipableGearStorage EquippedGearStorage { get; }

        public Storage(CharacterModel model)
        {
            using ItemContext context = new();

            EquippedBattleFashionStorage = new(GetItems(context, model, StorageType.EquippedBattleFashion));
            EquippedViewFashionStorage = new(GetItems(context, model, StorageType.EquippedViewFashion));
            EquippedGearStorage = new(GetItems(context, model, StorageType.EquippedGear));
        }

        private static IReadOnlyList<ItemStorage> GetItems(ItemContext context, CharacterModel model, StorageType type)
        {
            ItemStorage[] items = Enumerable.Repeat<ItemStorage>(null, Defines.FashionRows).ToArray();

            foreach (ItemModel item in context.Items.AsNoTracking().Where(c => c.CharacterId == model.Id && c.StorageType == type))
                items[item.SlotId] = new(item);

            return items;
        }
    }
}