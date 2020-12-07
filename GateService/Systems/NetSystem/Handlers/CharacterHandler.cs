using Core;
using Core.Systems.DatabaseSystem.Characters;
using Core.Systems.GameSystem.Datas.Bin.Table;
using Core.Systems.GameSystem.Datas.Bin.Table.Entities;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Character;
using GateService.Systems.GameSystem;
using SoulWorker.Types;
using System;
using System.Linq;

namespace GateService.Systems.NetSystem.Handlers
{
    internal static class CharacterHandler
    {
        [Handler(HandlerOpcode.CharacterChangeSlot, HandlerPermission.Authorized)]
        public static void ChangeSlot(Session session, ChangeSlotRequest request)
        {
            if (1 > request.FirstSlot || request.FirstSlot > Constants.CharactersSlotsCount)
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            if (1 > request.SecondSlot || request.SecondSlot > Constants.CharactersSlotsCount)
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

        [Handler(HandlerOpcode.CharacterCreate, HandlerPermission.Authorized)]
        public static void Create(Session session, CreateRequest request, Gate gate, ICustomizeHairTable customizeHairTable, ICustomizeEyesTable customizeEyesTable)
        {
            ///* Validate nickname */
            if (request.Character.Main.Name.Length > Constants.MaxCharacterNameLength)
            {
                return;
            }

            if (request.Character.Main.Name.Length < Constants.MinCharacterNameLength)
            {
                return;
            }

            ///* Validate hero */
            if (!Enum.IsDefined(typeof(HeroType), request.Character.Main.Character))
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            using var context = new CharacterContext();

            ///* Slot is busy, client error */
            if (context.Characters.Any(c => c.SlotId == request.SlotId && c.AccountId == session.Account.Id))
            {
#if !DEBUG
                session.Disconnect();
# endif
                return;
            }

            ///* Nickname is busy */
            if (context.Characters.Any(c => c.Name == request.Character.Main.Name)) { return; }

            CustomizeHairTableEntity customizeHair = customizeHairTable.ElementAtOrDefault((byte)request.Character.Main.Character);
            if (customizeHair is null)
            {
#if !DEBUG
                session.Disconnect();
# endif
                return;
            }

            ///* Validate hair style */
            if (!customizeHair.Style.Contains(request.Character.Main.Appearance.Hair.Style)) { return; }

            ///* Validate hair color */
            if (!customizeHair.Color.Contains(request.Character.Main.Appearance.Hair.Color)) { return; }

            CustomizeEyesTableEntity customizeEyes = customizeEyesTable[request.Character.Main.Character];

            ///* Validate eyes color */
            if (!customizeEyes.Color.Contains(request.Character.Main.Appearance.EyeColor)) { return; }

            //var classInfo = ClassSelectInfoTable.Instance[request.Character.Main.Character];
            ///* TODO: Add default outfit */

            //var info = CharacterInfoTable.Instance[(ushort)(1000 * (byte)request.Character.Main.Character)];

            ///* Validate outfit */
            //if (!info.DefaultCostume.Contains(request.Outfit)) { return; }

            //var model = CreateCharacterHelper.CreateModel(account, request, gateInfo);

            //context.Add(model);
            //context.SaveChanges();

            //var characters = session.GetComponent<CharacterSlots>();
            //var character = new Datas.Character(model);

            //characters[request.Slot].Character = character;
            //characters.Last = character;

            session.SendCharactersList();
        }

        [Handler(HandlerOpcode.CharacterDelete, HandlerPermission.Authorized)]
        public static void Delete(Session session, DeleteRequest request)
        {
            Character character = session.Characters.FirstOrDefault(character => character?.Id == request.Id);
            if (character is null) { return; }

            using var context = new CharacterContext();
            context.Characters.Remove(new() { Id = request.Id });
            context.SaveChanges();

            session.Characters[character.SlotId] = null;
            if (character.Id == session.Characters.LastSelectedId)
            {
                Character last = session.Characters.FirstOrDefault(character => character is not null);
                if (last is not null) { session.Characters.LastSelectedId = last.Id; }
            }

            session.SendCharactersList();
        }

        [Handler(HandlerOpcode.CharacterList, HandlerPermission.Authorized)]
        public static void GetList(Session session) => session.SendCharactersList();

        [Handler(HandlerOpcode.CharacterMarkFavorite, HandlerPermission.Authorized)]
        public static void MarkFavorite(Session session, MarkFavoriteRequest request)
        {
        }

        [Handler(HandlerOpcode.CharacterSelect, HandlerPermission.Authorized)]
        public static void Select(Session session, SelectRequest request, District district)
        {
            Character character = session.Characters.First(character => character?.Id == request.Id);
            if (character is null) { return; }

            session.Characters.LastSelectedId = character.Id;
            session.SendCharacterSelect(character, district);
        }

        [Handler(HandlerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptionList()
        {
        }
    }
}