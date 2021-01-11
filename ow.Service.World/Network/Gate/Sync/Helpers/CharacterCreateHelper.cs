using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.Utils;
using ow.Service.World.Game.Gate;
using System;
using System.Linq;

namespace ow.Service.World.Network.Gate.Sync.Helpers
{
    internal static class CharacterCreateHelper
    {
        internal static void ValidateHero(in SCharacterCreateRequest request)
        {
            if (request.Character.Main.Hero == Hero.None || !Enum.IsDefined(typeof(Hero), request.Character.Main.Hero))
                NetworkUtils.DropBadAction();
        }

        internal static void ValidateHair(in SCharacterCreateRequest request, BinTables binTable)
        {
            if (!binTable.CustomizeHair.TryGetValue(request.Character.Main.Hero, out CustomizeHairTableEntity? entity))
                NetworkUtils.DropBadAction();

            if (request.Character.Main.Appearance.Hair.Style == 0 || !entity!.Style.Contains(request.Character.Main.Appearance.Hair.Style))
                NetworkUtils.DropBadAction();
        }

        internal static void ValidateEyes(in SCharacterCreateRequest request, BinTables binTable)
        {
            if (!binTable.CustomizeEyes.TryGetValue(request.Character.Main.Hero, out CustomizeEyesTableEntity? entity))
                NetworkUtils.DropBadAction();

            if (request.Character.Main.Appearance.EyesColor == 0 || !entity!.Color.Contains(request.Character.Main.Appearance.EyesColor))
                NetworkUtils.DropBadAction();
        }

        internal static void ValidateSkin(in SCharacterCreateRequest request, BinTables binTable)
        {
            if (!binTable.CustomizeSkin.TryGetValue(request.Character.Main.Hero, out CustomizeSkinTableEntity? entity))
                NetworkUtils.DropBadAction();

            if (request.Character.Main.Appearance.SkinColor == 0 || !entity!.Color.Contains(request.Character.Main.Appearance.SkinColor))
                NetworkUtils.DropBadAction();
        }

        internal static void ValidateOutfit(in SCharacterCreateRequest request, BinTables binTable)
        {
            ///
            /// [ TODO ] Find where placed fucking id
            ///

            if (!binTable.CharacterInfo.TryGetValue((ushort)(1000 * (byte)request.Character.Main.Hero), out CharacterInfoTableEntity? entity))
                NetworkUtils.DropBadAction();

            if (request.Outfit == 0 || !entity!.DefaultOutfits.Contains(request.Outfit))
                NetworkUtils.DropBadAction();
        }

        public static CharacterModel CreateModel(Account account, SCharacterCreateRequest request, Instance gate, BinTables binTable) =>
            new()
            {
                AccountId = account.Id,
                Gate = gate.Id,
                Slot = request.Slot,
                Name = request.Character.Main.Name,
                Hero = request.Character.Main.Hero,
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
                Place = new() { Position = new() { X = 10444.9951f, Y = 10179.7461f, Z = 100.325394f }, Rotation = 0, Location = 10003 },
                Storage = CreateStorageInfo(),
                Bank = new(),
                Inventory = new(),
                LearnedSkill = Array.Empty<uint>(),
                QuickSlot = Enumerable.Repeat<uint>(0, Defines.SkillsInQuickSlotsCount).ToArray(),
                Energy = new(),
                Title = new(),
                Profile = new(),
                Gestures = new uint[Defines.QuickSlotsCount],
                Photo = binTable.PhotoItem.Values.First(c => c.Hero == request.Character.Main.Hero && c.PromotionInfo == 1).Id
            };

        internal static StorageModel[] CreateStorageInfo() => new StorageModel[]
            {
                new() { Type = StorageType.EquippedBattleFashion },
                new() { Type = StorageType.EquippedViewFashion },
                new() { Type = StorageType.EquippedGear },
                new() { Type = StorageType.InventoryItems },
                new() { Type = StorageType.InventoryFashion },
                new() { Type = StorageType.InventoryExtra },
                new() { Type = StorageType.BankItems },
                new() { Type = StorageType.BankFashion },
                new() { Type = StorageType.BankExtra },
            };
    }
}