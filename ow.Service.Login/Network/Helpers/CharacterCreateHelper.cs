using ow.Framework.Database.Characters;
using ow.Service.Login.Game.Gate;
using SoulCore;
using SoulCore.Data.Bin.Table.Entities;
using SoulCore.IO.Network.Requests;
using SoulCore.Types;
using System;
using System.Linq;

namespace ow.Service.Login.Network.Helpers
{
    internal static class CharacterCreateHelper
    {
        public static ushort CharacterOffset { get; } = 1000;

        public static CharacterModel CreateModel(Account account, SCharacterCreateRequest request, Instance instance, CharacterInfoEntity characterInfoEntity, PhotoItemEntity photoItemEntity) =>
             new()
             {
                 AccountId = account.Id,
                 Gate = instance.Id,
                 Slot = request.Slot,
                 Name = request.Character.Main.Name,
                 Class = request.Character.Main.Class,
                 Appearance = new()
                 {
                     Hair = new()
                     {
                         Style = request.Character.Main.Appearance.Hair.Style,
                         Color = request.Character.Main.Appearance.Hair.Color
                     },
                     EyeColor = request.Character.Main.Appearance.EyesColor,
                     SkinColor = request.Character.Main.Appearance.SkinColor,
                     EquippedHair = new()
                     {
                         Style = request.Character.Main.Appearance.EquippedHair.Style,
                         Color = request.Character.Main.Appearance.EquippedHair.Color
                     },
                     EquippedEyeColor = request.Character.Main.Appearance.EquippedEyesColor,
                     EquippedSkinColor = request.Character.Main.Appearance.EquippedSkinColor,
                 },
                 Place = new()
                 {
                     Position = new()
                     {
                         X = characterInfoEntity.DistrictPositionX,
                         Y = characterInfoEntity.DistrictPositionY,
                         Z = characterInfoEntity.DistrictPositionZ
                     },
                     Rotation = 0,
                     Location = (ushort)characterInfoEntity.District
                 },
                 Storage = CreateStorageInfo(),
                 Bank = new(),
                 Inventory = new(),
                 LearnedSkill = Array.Empty<uint>(),
                 QuickSlot = Enumerable.Repeat<uint>(0, Defines.SkillsInQuickSlotsCount).ToArray(),
                 Energy = new(),
                 Title = new(),
                 Profile = new(),
                 Gestures = new uint[Defines.QuickSlotsCount],
                 Photo = photoItemEntity.Id
             };

        internal static StorageModel[] CreateStorageInfo() => new StorageModel[]
            {
                new() { Type = StorageType.EquippedBattleFashion },
                new() { Type = StorageType.EquippedViewFashion },
                new() { Type = StorageType.EquippedGear },
                new() { Type = StorageType.InventoryItems },
                new() { Type = StorageType.InventoryFashion },
                new() { Type = StorageType.InventoryExtra },
                new() { Type = StorageType.BankCommon},
                new() { Type = StorageType.BankFashion },
                new() { Type = StorageType.BankCash},
            };
    }
}