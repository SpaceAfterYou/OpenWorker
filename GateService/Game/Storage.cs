using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Types;
using System.Collections.Generic;
using System.Linq;

namespace GateService.Game
{
    public sealed class Storage
    {
        public IReadOnlyList<Item> EquippedBattleFashionStorage { get; init; }
        public IReadOnlyList<Item> EquippedViewFashionStorage { get; init; }
        public IGearEquippedStorage EquippedGearStorage { get; init; }

        public Storage(CharacterModel model)
        {
            using ItemContext context = new();

            EquippedBattleFashionStorage = GetItems(context, model, StorageType.EquippedBattleFashion);
            EquippedViewFashionStorage = GetItems(context, model, StorageType.EquippedViewFashion);
            EquippedGearStorage = GetItems(context, model, StorageType.EquippedGear) as IGearEquippedStorage;
        }

        private static IReadOnlyList<Item> GetItems(ItemContext context, CharacterModel model, StorageType type)
        {
            Item[] items = Enumerable.Repeat<Item>(null, Defines.FashionRows).ToArray();

            foreach (var item in context.Items.AsNoTracking().Where(c => c.CharacterId == model.Id && c.StorageType == type))
            {
                items[item.SlotId] = new(item);
            }

            return items;
        }
    }
}