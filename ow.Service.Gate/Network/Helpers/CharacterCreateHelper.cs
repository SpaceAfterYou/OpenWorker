using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Requests.Character;
using ow.Service.Gate.Game;
using System;
using System.Linq;

namespace ow.Service.Gate.Network.Helpers
{
    internal static class CharacterCreateHelper
    {
        internal static void ValidateHero(in CreateRequest request)
        {
            if (request.Character.Main.Hero == Hero.None || !Enum.IsDefined(typeof(Hero), request.Character.Main.Hero))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif
        }

        internal static void ValidateHair(in CreateRequest request, BinTables binTable)
        {
            if (!binTable.CustomizeHairTable.TryGetValue(request.Character.Main.Hero, out ICustomizeHairTableEntity entity))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            if (request.Character.Main.Appearance.Hair.Style == 0 || !entity.Style.Contains(request.Character.Main.Appearance.Hair.Style))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif
        }

        internal static void ValidateEyes(in CreateRequest request, BinTables binTable)
        {
            if (!binTable.CustomizeEyesTable.TryGetValue(request.Character.Main.Hero, out ICustomizeEyesTableEntity entity))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            if (request.Character.Main.Appearance.EyesColor == 0 || !entity.Color.Contains(request.Character.Main.Appearance.EyesColor))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif
        }

        internal static void ValidateSkin(in CreateRequest request, BinTables binTable)
        {
            if (!binTable.CustomizeSkinTable.TryGetValue(request.Character.Main.Hero, out ICustomizeSkinTableEntity entity))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            if (request.Character.Main.Appearance.SkinColor == 0 || !entity.Color.Contains(request.Character.Main.Appearance.SkinColor))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif
        }

        internal static void ValidateOutfit(in CreateRequest request, BinTables binTable)
        {
            ///
            /// [ TODO ] Find where placed fucking id
            ///

            if (!binTable.CharacterInfoTable.TryGetValue((ushort)(1000 * (byte)request.Character.Main.Hero), out ICharacterInfoTableEntity entity))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            if (request.OutfitId == 0 || !entity.DefaultOutfits.Contains(request.OutfitId))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif
        }

        public static CharacterModel CreateModel(Session session, CreateRequest request, GateInfo gate, BinTables binTable) =>
            new()
            {
                AccountId = session.Account.Id,
                GateId = gate.Id,
                SlotId = request.SlotId,
                Name = request.Character.Main.Name,
                Hero = request.Character.Main.Hero,
                Appearance = new ApperanceModel()
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
                GeturesIds = new uint[Defines.QuickSlotsCount],
                PhotoId = binTable.PhotoItemTable.Values.First(c => c.Hero == request.Character.Main.Hero && c.Unknown14 == 1).Id
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