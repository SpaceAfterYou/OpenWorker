using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.FS.Enums;
using ow.Framework.FS.Storage;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Gate.Game
{
    public sealed class GateStorageEntity : IStorageEntity
    {
        public EquipableBattleFashionStorage EquippedBattleFashionStorage { get; }
        public EquipableViewFashionStorage EquippedViewFashionStorage { get; }
        public EquipableGearStorage EquippedGearStorage { get; }

        public GateStorageEntity()
        {
            EquippedBattleFashionStorage = new(Enumerable.Empty<ItemModel>());
            EquippedViewFashionStorage = new(Enumerable.Empty<ItemModel>());
            EquippedGearStorage = new(Enumerable.Empty<ItemModel>());
        }

        public GateStorageEntity(CharacterModel model)
        {
            using ItemContext context = new();

            EquippedBattleFashionStorage = new(GetItems(context, model, StorageType.EquippedBattleFashion));
            EquippedViewFashionStorage = new(GetItems(context, model, StorageType.EquippedViewFashion));
            EquippedGearStorage = new(GetItems(context, model, StorageType.EquippedGear));
        }

        private static IEnumerable<ItemModel> GetItems(ItemContext context, CharacterModel model, StorageType type)
        {
            foreach (ItemModel itemModel in context.Items.AsNoTracking().Where(c => c.CharacterId == model.Id && c.StorageType == type))
                yield return itemModel;
        }
    }
}