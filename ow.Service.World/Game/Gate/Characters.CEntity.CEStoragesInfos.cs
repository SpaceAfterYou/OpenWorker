using Microsoft.EntityFrameworkCore;
using SoulCore.Database.Characters;
using SoulCore.Database.Storages;
using SoulCore.Game.Datas.Bin.Table.Entities;
using SoulCore.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.World.Game.Gate
{
    internal sealed partial class Characters
    {
        internal sealed partial class CEntity
        {
            internal sealed partial record CEStoragesInfos
            {
                internal CESIEquipableFashionStorage EquippedBattleFashion { get; }
                internal CESIEquipableFashionStorage EquippedViewFashion { get; }
                internal CESIEquipableGearStorageItem EquippedGearWeapon { get; }

                internal CEStoragesInfos(CharacterModel model, ItemContext context, CharacterInfoTableEntity characterInfo, BinTables tables)
                {
                    EquippedBattleFashion = new(Enumerable.Empty<ItemModel>());
                    EquippedViewFashion = new(Enumerable.Empty<ItemModel>());
                    EquippedGearWeapon = CESIEquipableGearStorageItem.Empty;

                    //ItemTableEntity weaponItem = tables.Item[characterInfo.DefaultWeaponId];
                    //ItemClassifyTableEntity weaponClassify = tables.ItemClassify[weaponItem.ClassifyId];
                    //if (weaponClassify.SocketId != 0)
                    //{
                    //    if (weaponClassify.UseType == 1)
                    //    {
                    //    }
                    //}

                    //EquippedGearWeapon = CESIEquipableGearStorageItem.Empty;
                }

                internal CEStoragesInfos(CharacterModel model, ItemContext context)
                {
                    EquippedBattleFashion = new(GetItems(context, model, StorageType.EquippedBattleFashion));
                    EquippedViewFashion = new(GetItems(context, model, StorageType.EquippedViewFashion));

                    {
                        ItemModel? itemModel = context.Items
                            .AsNoTracking()
                            .FirstOrDefault(s => s.CharacterId == model.Id && s.StorageType == StorageType.EquippedGear && s.Slot == (ushort)StorageEquippedGearSlot.Weapon);

                        EquippedGearWeapon = itemModel is not null ? new CESIEquipableGearStorageItem(itemModel) : CESIEquipableGearStorageItem.Empty;
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
