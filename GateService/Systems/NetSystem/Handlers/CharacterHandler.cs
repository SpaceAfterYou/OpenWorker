using Core.DatabaseSystem.Characters;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Character;
using GateService.Systems.GameSystem;
using System.Linq;

namespace GateService.Systems.NetSystem.Handlers
{
    internal static class CharacterHandler
    {
        [Handler(HandlerOpcode.CharacterChangeSlot, HandlerPermission.Authorized)]
        public static void ChangeSlot(Session session, ChangeSlotRequest request)
        {
            //if (1 > request.FirstSlot || request.FirstSlot > SoulWorker.Constants.CharactersSlotsCount)
            //{
            //    session.Disconnect();
            //    return;
            //}

            //if (1 > request.SecondSlot || request.SecondSlot > SoulWorker.Constants.CharactersSlotsCount)
            //{
            //    session.Disconnect();
            //    return;
            //}

            //if (request.FirstSlot == request.SecondSlot)
            //{
            //    session.Disconnect();
            //    return;
            //}

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
        public static void Create(Session session, CreateRequest request, Gate gate)
        {
            ///* Validate nickname */
            //if (request.Character.Main.Name.Length > SoulWorker.Constants.MaxCharacterNameLength || request.Character.Main.Name.Length < SoulWorker.Constants.MinCharacterNameLength)
            //{
            //    return;
            //}

            ///* Validate hero */
            //if (!Enum.IsDefined(typeof(HeroType), request.Character.Main.Character)) { return; }

            //using var context = new CharacterContext();

            ///* Slot is busy, client error */
            //if (context.Characters.Any(c => c.SlotId == request.Slot && c.AccountId == account.Id)) { return; }

            ///* Nickname is busy */
            //if (context.Characters.Any(c => c.Name == request.Character.Main.Name)) { return; }

            //var customizeHair = CustomizeHairTable.Instance[request.Character.Main.Character];

            ///* Validate hair style */
            //if (!customizeHair.Style.Contains(request.Character.Main.Appearance.Hair.Style)) { return; }

            ///* Validate hair color */
            //if (!customizeHair.Color.Contains(request.Character.Main.Appearance.Hair.Color)) { return; }

            //var customizeEyes = CustomizeEyesTable.Instance[request.Character.Main.Character];

            ///* Validate eyes color */
            //if (!customizeEyes.Color.Contains(request.Character.Main.Appearance.EyeColor)) { return; }

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