using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Character;
using ow.Service.Gate.Game;
using ow.Service.Gate.Network.Extensions;
using ow.Service.Gate.Network.Helpers;
using System.Linq;

namespace ow.Service.Gate.Network.Handlers
{
    internal static class CharacterHandler
    {
        [Handler(ServerOpcode.CharacterChangeSlot, HandlerPermission.Authorized)]
        public static void ChangeSlot(GameSession session, ChangeSlotRequest request)
        {
            if (1 > request.FirstSlot || request.FirstSlot > Defines.CharactersSlotsCount)
#if !DEBUG
                throw new BadActionException();
#endif
                return;

            if (1 > request.SecondSlot || request.SecondSlot > Defines.CharactersSlotsCount)
#if !DEBUG
                throw new BadActionException();
#endif
                return;

            if (request.FirstSlot == request.SecondSlot)
#if !DEBUG
                throw new BadActionException();
#endif
                return;

            //var first = slots.FirstOrDefault(slot => slot.Id == request.FirstSlot);
            //var second = slots.FirstOrDefault(slot => slot.Id == request.SecondSlot);

            ///* No characters found */
            //if (first is null && second is null)
            //#if !DEBUG
            //                throw new BadActionException();
            //#endif
            //            return;

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
        public static void Create(GameSession session, CreateRequest request, GateInfo gate, BinTables binTable)
        {
            if (request.Character.Main.Name.Length > Defines.MaxCharacterNameLength)
                return;

            if (request.Character.Main.Name.Length < Defines.MinCharacterNameLength)
                return;

            CharacterCreateHelper.ValidateHero(request);
            CharacterCreateHelper.ValidateHair(request, binTable);
            CharacterCreateHelper.ValidateEyes(request, binTable);
            CharacterCreateHelper.ValidateSkin(request, binTable);
            CharacterCreateHelper.ValidateOutfit(request, binTable);

            using CharacterContext context = new();

            if (context.Characters.Any(c => c.SlotId == request.SlotId && c.AccountId == session.Account.Id))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            if (context.Characters.Any(c => c.Name == request.Character.Main.Name))
                return;

            if (!binTable.ClassSelectInfoTable.TryGetValue(request.Character.Main.Hero, out IClassSelectInfoTableEntity classInfo))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            /// [ TODO ] Add default items to inventory

            CharacterModel model = CharacterCreateHelper.CreateModel(session, request, gate, binTable);
            context.UseAndSave(c => c.Add(model));

            Character character = new(model, binTable);

            session.Characters[request.SlotId] = character;
            session.Characters.LastSelected = character;

            session.SendCharactersList();
        }

        [Handler(ServerOpcode.CharacterDelete, HandlerPermission.Authorized)]
        public static void Delete(GameSession session, DeleteRequest request)
        {
            Character character = session.Characters.Find(character => character?.Id == request.Id);

            if (character is null)
                return;

            using CharacterContext context = new();
            context.UseAndSave(c => c.Remove<CharacterModel>(new() { Id = request.Id }));

            session.Characters[character.Slot] = null;

            if (character.Id == session.Characters.LastSelected?.Id)
                session.Characters.LastSelected = session.Characters.Find(character => character is not null);

            if (character.Id == session.Characters.Favorite?.Id)
                session.Characters.Favorite = null;

            session.SendCharactersList();
        }

        [Handler(ServerOpcode.CharacterList, HandlerPermission.Authorized)]
        public static void GetList(GameSession session) => session.SendCharactersList();

        [Handler(ServerOpcode.CharacterMarkFavorite, HandlerPermission.Authorized)]
        public static void MarkFavorite(GameSession session, MarkFavoriteRequest request)
        {
            Character character = session.Characters.Find(c => c?.Id == request.CharacterId);
            if (character is null)
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            session.Characters.Favorite = character;
            session.SendFavoriteCharacter();
        }

        [Handler(ServerOpcode.CharacterSelect, HandlerPermission.Authorized)]
        public static void Select(GameSession session, SelectRequest request, DistrictInstance district)
        {
            Character character = session.Characters.Find(character => character?.Id == request.Id);
            if (character is null)
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            session.Characters.LastSelected = character;
            session.SendCharacterSelect(character, district);
        }

        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptionList()
        {
        }

        [Handler(ServerOpcode.CharacterChangeBackground, HandlerPermission.Authorized)]
        public static void ChangeBackground(GameSession session, ChangeBackgroundRequest request, BinTables binTable)
        {
            if (!binTable.CharacterBackgroundTable.TryGetValue(request.BackgroundId, out ICharacterBackgroundTableEntity entity))
#if !DEBUG
                throw new BadActionException();
#else
                return;
#endif

            session.Entity.Set(entity);
            session.SendCharacterBackground(entity);
        }
    }
}