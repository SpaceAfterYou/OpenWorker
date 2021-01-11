using Microsoft.EntityFrameworkCore;
using ow.Framework;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace ow.Service.World.Game.Gate
{
    internal sealed class Characters : Dictionary<int, Characters.CharacterEntity>
    {
        internal sealed class CharacterEntity
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
                internal sealed class EquipableFashionStorage : List<EquipableFashionStorage.Item?>
                {
                    internal sealed record Item
                    {
                        internal int PrototypeId { get; init; }
                        internal uint Color { get; init; }
                    }

                    internal EquipableFashionStorage(IEnumerable<ItemModel> value) : base(GetItems(value))
                    {
                    }

                    private static IEnumerable<Item?> GetItems(IEnumerable<ItemModel> value)
                    {
                        Item?[] items = Enumerable.Repeat((Item?)null, Defines.EquipableFashionStorageMaxCapacity).ToArray();

                        foreach (ItemModel model in value)
                            items[model.Slot] = new Item()
                            {
                                PrototypeId = (int)model.PrototypeId,
                                Color = model.Color
                            };

                        return items;
                    }
                }

                internal sealed record EquipableGearStorageItem
                {
                    internal int PrototypeId { get; init; }
                    internal byte UpgradeLevel { get; init; }

                    internal static EquipableGearStorageItem Empty => _empty;

                    private static readonly EquipableGearStorageItem _empty = new()
                    {
                        PrototypeId = -1
                    };

                    internal EquipableGearStorageItem()
                    {
                    }

                    internal EquipableGearStorageItem(ItemModel model)
                    {
                        PrototypeId = (int)model.PrototypeId;
                        UpgradeLevel = model.Upgrade.CurrentLevel;
                    }
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

                internal StoragesInfos(CharacterModel model, ItemContext context)
                {
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

            internal CharacterEntity(CharacterModel model, BinTables tables, ItemContext context)
            {
                Id = model.Id;
                Name = model.Name;
                Photo = model.Photo;
                Storages = new(model, context);
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

        public TimeSpan InitializeTime { get; }
        public CharacterEntity? Favorite { get; set; }
        public CharacterEntity? LastSelected { get; set; }

        public Characters(AccountModel accountModel, ushort gateId, BinTables tables, CharacterContext characterContext, ItemContext itemContext)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            foreach (CharacterModel model in GetCharacterModels(accountModel, gateId, characterContext))
                if (!TryAdd(model.Id, new(model, tables, itemContext)))
                    NetworkUtils.DropBadAction();

            if (accountModel.LastSelectedCharacter != -1 && TryGetValue(accountModel.LastSelectedCharacter, out CharacterEntity? last))
                LastSelected = last;

            if (accountModel.FavoriteCharacter != -1 && TryGetValue(accountModel.FavoriteCharacter, out CharacterEntity? favorite))
                Favorite = favorite;

            stopwatch.Stop();
            InitializeTime = stopwatch.Elapsed;
        }

        private static IEnumerable<CharacterModel> GetCharacterModels(AccountModel accountModel, ushort gateId, CharacterContext context)
        {
            foreach (CharacterModel model in context.Characters.AsNoTracking().Where(c => c.AccountId == accountModel.Id && c.Gate == gateId))
                yield return model;
        }
    }
}