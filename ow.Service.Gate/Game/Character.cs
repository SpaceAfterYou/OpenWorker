using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ow.Service.Gate.Game
{
    internal sealed class Character
    {
        internal readonly struct AppearanceInfo
        {
            internal readonly struct HairInfo
            {
                internal ushort Style { get; init; }
                internal ushort Color { get; init; }
            }

            internal HairInfo Hair { get; init; }
            internal ushort EyeColor { get; init; }
            internal ushort SkinColor { get; init; }
            internal HairInfo EquippedHair { get; init; }
            internal ushort EquippedEyeColor { get; init; }
            internal ushort EquippedSkinColor { get; init; }
        }

        internal readonly struct PlaceInfo
        {
            internal Vector3 Postion { get; init; }
            internal float Rotation { get; init; }
            internal ushort District { get; init; }

            internal PlaceInfo(PlaceModel model, BinTables tables)
            {
                Postion = new Vector3(model.Position.X, model.Position.Y, model.Position.Z);
                Rotation = model.Rotation;
                District = model.Location;
            }
        }

        internal sealed record StoragesInfos
        {
            internal sealed class EquipableFashionStorage : List<EquipableFashionStorage.Item>
            {
                internal sealed record Item
                {
                    internal int PrototypeId { get; init; }
                    internal uint Color { get; init; }

                    internal static Item Empty => _empty;

                    private static readonly Item _empty = new()
                    {
                        PrototypeId = -1
                    };
                }

                internal EquipableFashionStorage(IEnumerable<ItemModel> value) : base(GetItems(value))
                {
                }

                private static IEnumerable<Item> GetItems(IEnumerable<ItemModel> value)
                {
                    Item[] items = Enumerable.Repeat(Item.Empty, Defines.EquipableFashionStorageMaxCapacity).ToArray();

                    foreach (ItemModel model in value)
                        items[model.Slot] = new Item()
                        {
                            PrototypeId = model.PrototypeId,
                            Color = model.Color
                        };

                    return items;
                }
            }

            internal sealed record EquipableGearStorageItem
            {
                internal int PrototypeId { get; init; }
                internal byte UpgradeLevel { get; init; }

                internal EquipableGearStorageItem()
                {
                }

                internal EquipableGearStorageItem(ItemModel model)
                {
                    PrototypeId = model.PrototypeId;
                    UpgradeLevel = model.Upgrade.CurrentLevel;
                }

                internal static EquipableGearStorageItem Empty => _empty;

                private static readonly EquipableGearStorageItem _empty = new()
                {
                    PrototypeId = -1
                };
            }

            internal EquipableFashionStorage EquippedBattleFashion { get; }
            internal EquipableFashionStorage EquippedViewFashion { get; }
            internal EquipableGearStorageItem EquippedGearWeapon { get; }

            internal StoragesInfos()
            {
                EquippedBattleFashion = new(Enumerable.Empty<ItemModel>());
                EquippedViewFashion = new(Enumerable.Empty<ItemModel>());
                EquippedGearWeapon = EquipableGearStorageItem.Empty;
            }

            internal StoragesInfos(CharacterModel model)
            {
                using ItemContext context = new();

                EquippedBattleFashion = new(GetItems(context, model, StorageType.EquippedBattleFashion));
                EquippedViewFashion = new(GetItems(context, model, StorageType.EquippedViewFashion));

                {
                    ItemModel? itemModel = context.Items
                        .AsNoTracking()
                        .FirstOrDefault(s => s.CharacterId == model.Id && s.StorageType == StorageType.EquippedGear && s.Slot == (ushort)EquippedGearSlot.Weapon);

                    EquippedGearWeapon = itemModel is not null ? new EquipableGearStorageItem(itemModel) : EquipableGearStorageItem.Empty;
                }
            }

            private static IEnumerable<ItemModel> GetItems(ItemContext context, CharacterModel model, StorageType type)
            {
                foreach (ItemModel itemModel in context.Items.AsNoTracking().Where(c => c.CharacterId == model.Id && c.StorageType == type))
                    yield return itemModel;
            }
        }

        internal int Id { get; init; }
        internal string Name { get; init; }
        internal uint Photo { get; init; }
        internal StoragesInfos Storages { get; init; }
        internal PlaceInfo Place { get; init; }
        internal byte Slot { get; set; }
        internal CharacterAdvancement Advancement { get; init; }
        internal Hero Hero { get; init; }
        internal byte Level { get; init; }
        internal AppearanceInfo Appearance { get; init; }

        internal Character(CharacterModel model, BinTables tables)
        {
            Id = model.Id;
            Name = model.Name;
            Photo = model.Photo;
            Storages = new(model);
            Place = new(model.Place, tables);
            Slot = model.Slot;
            Advancement = model.Advancement;
            Hero = model.Hero;
            Level = model.Level;
            Appearance = new()
            {
                EquippedEyeColor = model.Appearance.EquippedEyeColor,
                EquippedHair = new()
                {
                    Color = model.Appearance.EquippedHair.Color,
                    Style = model.Appearance.EquippedHair.Style,
                },
                Hair = new()
                {
                    Style = model.Appearance.Hair.Style,
                    Color = model.Appearance.Hair.Color,
                },
                EquippedSkinColor = model.Appearance.EquippedSkinColor,
                EyeColor = model.Appearance.EyeColor,
                SkinColor = model.Appearance.SkinColor,
            };
        }
    }
}