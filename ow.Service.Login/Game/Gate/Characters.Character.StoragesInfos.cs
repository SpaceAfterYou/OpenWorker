using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using SoulCore.Data.Bin.Table.Entities;
using SoulCore.Types;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal sealed partial record StoragesInfos
            {
                internal CostumeItems EquippedBattleFashion { get; }
                internal CostumeItems EquippedViewFashion { get; }
                internal GearItem EquippedGearWeapon { get; }

                internal StoragesInfos(CharacterModel model, ItemContext context, CharacterInfoEntity characterInfo, BinTable tables)
                {
                    EquippedBattleFashion = new(Enumerable.Empty<ItemModel>());
                    EquippedViewFashion = new(Enumerable.Empty<ItemModel>());
                    EquippedGearWeapon = GearItem.Empty;

                    //ItemEntity weaponItem = tables.Item[characterInfo.DefaultWeaponId];
                    //ItemClassifyEntity weaponClassify = tables.ItemClassify[weaponItem.ClassifyId];
                    //if (weaponClassify.SocketId != 0)
                    //{
                    //    if (weaponClassify.UseType == 1)
                    //    {
                    //    }
                    //}

                    //EquippedGearWeapon = CESIEquipableGearStorageItem.Empty;
                }

                internal StoragesInfos(CharacterModel model, ItemContext context)
                {
                    EquippedBattleFashion = new(GetItems(context, model, StorageType.EquippedBattleFashion));
                    EquippedViewFashion = new(GetItems(context, model, StorageType.EquippedViewFashion));

                    {
                        ItemModel? itemModel = context.Items
                            .AsNoTracking()
                            .FirstOrDefault(s => s.CharacterId == model.Id && s.StorageType == StorageType.EquippedGear && s.Slot == (ushort)StorageEquippedGearSlotType.Weapon);

                        EquippedGearWeapon = itemModel is not null ? new GearItem(itemModel) : GearItem.Empty;
                    }
                }

                private static IEnumerable<ItemModel> GetItems(ItemContext context, CharacterModel model, StorageType type)
                {
                    foreach (ItemModel itemModel in context.Items.AsNoTracking().Where(c => c.CharacterId == model.Id && c.StorageType == type))
                        yield return itemModel;
                }
            }
        }
    }
}