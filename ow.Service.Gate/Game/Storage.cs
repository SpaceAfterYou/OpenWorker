using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game;
using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Gate.Game
{
    public sealed class Storage : IStorage
    {
        public IReadOnlyEquipableViewFashionStorage EquippedViewFashionStorage { get; }

        public IReadOnlyEquipableBattleFashionStorage EquippedBattleFashionStorage { get; }

        public IReadOnlyEquipableGearStorage EquippedGearStorage { get; }

        public Storage(CharacterModel model)
        {
            using ItemContext context = new();

            EquippedBattleFashionStorage = new EquipableStorage(GetItems(context, model, StorageType.EquippedBattleFashion));
            EquippedViewFashionStorage = new EquipableStorage(GetItems(context, model, StorageType.EquippedViewFashion));
            EquippedGearStorage = new EquipableStorage(GetItems(context, model, StorageType.EquippedGear));
        }

        private static IReadOnlyList<Item> GetItems(ItemContext context, CharacterModel model, StorageType type)
        {
            Item[] items = Enumerable.Repeat<Item>(null, Defines.FashionRows).ToArray();

            foreach (ItemModel item in context.Items.AsNoTracking().Where(c => c.CharacterId == model.Id && c.StorageType == type))
                items[item.SlotId] = new(item);

            return items;
        }
    }
}