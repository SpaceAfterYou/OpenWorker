using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Ids;
using ow.Framework.Game.Types;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Character;
using ow.Service.Gate.Game;
using System;
using System.Linq;

namespace ow.Service.Gate.Network.Handlers
{
    internal static class CharacterHandler
    {
        [Handler(ServerOpcode.CharacterChangeSlot, HandlerPermission.Authorized)]
        public static void ChangeSlot(Session session, ChangeSlotRequest request)
        {
            if (1 > request.FirstSlot || request.FirstSlot > Defines.CharactersSlotsCount)
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            if (1 > request.SecondSlot || request.SecondSlot > Defines.CharactersSlotsCount)
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            if (request.FirstSlot == request.SecondSlot)
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            //var first = slots.FirstOrDefault(slot => slot.Id == request.FirstSlot);
            //var second = slots.FirstOrDefault(slot => slot.Id == request.SecondSlot);

            ///* No characters found */
            //if (first is null && second is null)
            //{
            //    session.Disconnect();
            //    return;
            //}

            //if (second is not null && first is not null)
            //{
            //    SlotCharacterHelper.Swap(first, second);
            //    return;
            //}

            //if (first is not null)
            //{
            //    SlotCharacterHelper.Change(first, slots[request.SecondSlot]);
            //    return;
            //}

            //SlotCharacterHelper.Change(second, slots[request.FirstSlot]);
        }

        [Handler(ServerOpcode.CharacterCreate, HandlerPermission.Authorized)]
        public static void Create(Session session, CreateRequest request, GateInfo gate, BinTable binTable)
        {
            // Validate nickname
            if (request.Character.Main.Name.Length > Defines.MaxCharacterNameLength)
            {
                return;
            }

            if (request.Character.Main.Name.Length < Defines.MinCharacterNameLength)
            {
                return;
            }

            // Validate hero
            if (!Enum.IsDefined(typeof(HeroId), request.Character.Main.Hero))
            {
#if !DEBUG
                throw new CreateCharacterException();
#else
                return;
#endif
            }

            using var context = new CharacterContext();

            // Slot is busy, client error
            if (context.Characters.Any(c => c.SlotId == request.SlotId && c.AccountId == session.Account.Id))
            {
#if !DEBUG
                throw new CreateCharacterException();
#else
                return;
#endif
            }

            // Nickname is busy
            if (context.Characters.Any(c => c.Name == request.Character.Main.Name)) { return; }

            if (!binTable.CustomizeHairTable.TryGetValue(request.Character.Main.Hero, out CustomizeHairTableEntity customizeHair))
            {
#if !DEBUG
                throw new CreateCharacterException();
#else
                return;
#endif
            }

            // Validate hair style
            if (!customizeHair.Style.Contains(request.Character.Main.Appearance.Hair.Style)) { return; }

            // Validate hair color
            if (!customizeHair.Color.Contains(request.Character.Main.Appearance.Hair.Color)) { return; }

            if (!binTable.CustomizeEyesTable.TryGetValue(request.Character.Main.Hero, out CustomizeEyesTableEntity customizeEyes))
            {
#if !DEBUG
                throw new CreateCharacterException();
#else
                return;
#endif
            }

            // TODO: Check skin

            // Validate eyes color
            if (!customizeEyes.Color.Contains(request.Character.Main.Appearance.EyeColor)) { return; }

            if (!binTable.ClassSelectInfoTable.TryGetValue(request.Character.Main.Hero, out ClassSelectInfoTableEntity classInfo))
            {
#if !DEBUG
                throw new CreateCharacterException();
#else
                return;
#endif
            }

            // TODO: Find where placed id
            if (!binTable.CharacterInfoTable.TryGetValue((ushort)(1000 * (byte)request.Character.Main.Hero), out CharacterInfoTableEntity characterInfo))
            {
#if !DEBUG
                throw new CreateCharacterException();
#else
                return;
#endif
            }

            // Validate outfit
            if (!characterInfo.DefaultCostumeIds.Contains(request.OutfitId))
            {
#if !DEBUG
                throw new CreateCharacterException();
#else
                return;
#endif
            }

            // TODO: Add default outfit to inventory

            CharacterModel model = CreateModel(session, request, gate);
            context.UseAndSave(c => c.Add(model));

            Character character = new(model);

            session.Characters[request.SlotId] = character;
            session.Characters.LastSelected = character;

            session.SendCharactersList();
        }

        [Handler(ServerOpcode.CharacterDelete, HandlerPermission.Authorized)]
        public static void Delete(Session session, DeleteRequest request)
        {
            Character character = session.Characters.FirstOrDefault(character => character?.Id == request.Id);
            if (character is null) { return; }

            using CharacterContext context = new();
            context.UseAndSave(c => c.Remove<CharacterModel>(new() { Id = request.Id }));

            session.Characters[character.SlotId] = null;
            if (character.Id == session.Characters.LastSelected.Id)
            {
                Character firstAvailableCharacter = session.Characters.FirstOrDefault(character => character is not null);
                if (firstAvailableCharacter is not null) { session.Characters.LastSelected = firstAvailableCharacter; }
            }

            session.SendCharactersList();
        }

        [Handler(ServerOpcode.CharacterList, HandlerPermission.Authorized)]
        public static void GetList(Session session) => session.SendCharactersList();

        [Handler(ServerOpcode.CharacterMarkFavorite, HandlerPermission.Authorized)]
        public static void MarkFavorite(Session session, MarkFavoriteRequest request)
        {
        }

        [Handler(ServerOpcode.CharacterSelect, HandlerPermission.Authorized)]
        public static void Select(Session session, SelectRequest request, District district)
        {
            Character character = session.Characters.First(character => character?.Id == request.Id);
            if (character is null) { return; }

            session.Characters.LastSelected = character;
            session.SendCharacterSelect(character, district);
        }

        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptionList()
        {
        }

        public static CharacterModel CreateModel(Session session, in CreateRequest request, GateInfo gate) =>
            new()
            {
                AccountId = session.Account.Id,
                GateId = gate.Id,
                SlotId = request.SlotId,
                Name = request.Character.Main.Name,
                Hero = request.Character.Main.Hero,
                Gesture = new uint[6] {
                    (uint)((byte)request.Character.Main.Hero * 1000 + 0),
                    (uint)((byte)request.Character.Main.Hero * 1000 + 1),
                    (uint)((byte)request.Character.Main.Hero * 1000 + 2),
                    (uint)((byte)request.Character.Main.Hero * 1000 + 3),
                    (uint)((byte)request.Character.Main.Hero * 1000 + 4),
                    (uint)((byte)request.Character.Main.Hero * 1000 + 5)
                },
                Appearance = new ApperanceModel()
                {
                    Hair = new()
                    {
                        Style = request.Character.Main.Appearance.Hair.Style,
                        Color = request.Character.Main.Appearance.Hair.Color
                    },
                    EyeColor = request.Character.Main.Appearance.EyeColor,
                    SkinColor = request.Character.Main.Appearance.SkinColor,
                    EquippedHair = new()
                    {
                        Style = request.Character.Main.Appearance.EquippedHair.Style,
                        Color = request.Character.Main.Appearance.EquippedHair.Color
                    },
                    EquippedEyeColor = request.Character.Main.Appearance.EquippedEyeColor,
                    EquippedSkinColor = request.Character.Main.Appearance.EquippedSkinColor,
                },
                Place = new() { Position = new() { X = 10444.9951f, Y = 10179.7461f, Z = 100.325394f }, Rotation = 0, Location = 10003 },
                LearnedSkill = Array.Empty<uint>(),
                QuickSlot = Enumerable.Repeat<uint>(0, 6 * 3).ToArray(),
                Storage = CreateStorageInfo(),
                Energy = new EnergyModel() { Primary = 200, Extra = 0 },
                Profile = new ProfileModel() { Status = ProfileStatusType.Rookie, About = "", Note = "" },
                Title = new TitleModel() { Primary = 0, Secondary = 0 }
            };

        private static StorageModel[] CreateStorageInfo() => new StorageModel[]
            {
                new() { Type = StorageType.EquippedBattleFashion, Upgrades = 0 },
                new() { Type = StorageType.EquippedViewFashion, Upgrades = 0 },
                new() { Type = StorageType.EquippedGear, Upgrades = 0 },
                new() { Type = StorageType.InventoryItems, Upgrades = 0 },
                new() { Type = StorageType.InventoryFashion, Upgrades = 0 },
                new() { Type = StorageType.InventoryExtra, Upgrades = 0 },
                new() { Type = StorageType.BankItems, Upgrades = 0 },
                new() { Type = StorageType.BankFashion, Upgrades = 0 },
                new() { Type = StorageType.BankExtra, Upgrades = 0 },
            };
    }
}