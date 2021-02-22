using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using SoulCore.Game.Storage;
using SoulCore.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ow.Service.District.Game
{
    public sealed record Storages
    {
        internal static readonly Storages Empty = new();

        internal readonly EquipableBattleFashionStorage EquippedBattleFashion = EquipableBattleFashionStorage.Empty;
        internal readonly EquipableViewFashionStorage EquippedViewFashion = EquipableViewFashionStorage.Empty;
        internal readonly EquipableGearStorage EquippedGear = EquipableGearStorage.Empty;

        private async Task<Storages> Create(CharacterModel model, BinTable tables, ItemContext context)
        {
            EquipableBattleFashionStorage EquippedBattleFashion = new(GetItems(context, model, StorageType.EquippedBattleFashion), tables);
            EquipableViewFashionStorage EquippedViewFashion = new(GetItems(context, model, StorageType.EquippedViewFashion), tables);
            EquipableGearStorage EquippedGear = new(GetItems(context, model, StorageType.EquippedGear), tables);
        }

        public Storages(CharacterModel model, BinTable tables, ItemContext context)
        {
        }

        private Storages()
        {
        }

        private static async IAsyncEnumerable<ItemModel> GetItems(ItemContext context, CharacterModel model, StorageType type)
        {
            await foreach (ItemModel itemModel in context.Items.AsNoTracking().Where(c => c.CharacterId == model.Id && c.StorageType == type).AsAsyncEnumerable())
            {
                yield return itemModel;
            }
        }
    }
}