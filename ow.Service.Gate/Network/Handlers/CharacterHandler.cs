using DefaultEcs;
using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.FS.Datas.Bin.Table.Entities;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.Utils;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Repository;
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
                NetworkUtils.DropSession();

            if (1 > request.SecondSlot || request.SecondSlot > Defines.CharactersSlotsCount)
                NetworkUtils.DropSession();

            if (request.FirstSlot == request.SecondSlot)
                NetworkUtils.DropSession();

            //Characters characters = session.Entity.Get<Characters>();
            //characters.Values.First

            //var first = slots.FirstOrDefault(slot => slot.Id == request.FirstSlot);
            //var second = slots.FirstOrDefault(slot => slot.Id == request.SecondSlot);

            ///* No characters found */
            //if (first is null && second is null)
            //    NetworkUtils.DropSession();

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
        public static void Create(GameSession session, CreateRequest request, GateInstance gate, BinTables tables)
        {
            if (request.Character.Main.Name.Length > Defines.MaxCharacterNameLength)
                return;

            if (request.Character.Main.Name.Length < Defines.MinCharacterNameLength)
                return;

            CharacterCreateHelper.ValidateHero(request);
            CharacterCreateHelper.ValidateHair(request, tables);
            CharacterCreateHelper.ValidateEyes(request, tables);
            CharacterCreateHelper.ValidateSkin(request, tables);
            CharacterCreateHelper.ValidateOutfit(request, tables);

            using CharacterContext context = new();

            Account account = session.Entity.Get<Account>();
            if (context.Characters.Any(c => c.Slot == request.SlotId && c.AccountId == account.Id))
                NetworkUtils.DropSession();

            if (context.Characters.Any(c => c.Name == request.Character.Main.Name))
                return;

            if (!tables.ClassSelectInfoTable.TryGetValue(request.Character.Main.Hero, out ClassSelectInfoTableEntity classInfo))
                NetworkUtils.DropSession();

            /// [ TODO ] Add default items to inventory

            CharacterModel model = CharacterCreateHelper.CreateModel(account, request, gate, tables);
            context.UseAndSave(c => c.Add(model));

            Characters characters = session.Entity.Get<Characters>();
            characters.Create(model, tables);

            session.SendCharactersList();
        }

        [Handler(ServerOpcode.CharacterDelete, HandlerPermission.Authorized)]
        public static void Delete(GameSession session, DeleteRequest request)
        {
            Characters characters = session.Entity.Get<Characters>();
            characters.Delete(request.Id);

            //int slot = characters.FindIndex(c => c.Has<EntityCharacter>() && c.Get<EntityCharacter>().Id == request.Id);
            //if (slot == -1) NetworkUtils.DropSession();

            //characters.Remove(slot);

            using CharacterContext context = new();
            context.UseAndSave(c => c.Remove<CharacterModel>(new() { Id = request.Id }));

            session.SendCharactersList();
        }

        [Handler(ServerOpcode.CharacterList, HandlerPermission.Authorized)]
        public static void GetList(GameSession session) => session.SendCharactersList();

        [Handler(ServerOpcode.CharacterMarkFavorite, HandlerPermission.Authorized)]
        public static void MarkFavorite(GameSession session, MarkFavoriteRequest request)
        {
            Characters characters = session.Entity.Get<Characters>();

            if (!characters.TryGetValue(request.Id, out Entity entity))
                NetworkUtils.DropSession();

            characters.Favorite = entity;
            session.SendFavoriteCharacter();
        }

        [Handler(ServerOpcode.CharacterSelect, HandlerPermission.Authorized)]
        public static void Select(GameSession session, SelectRequest request, DistrictRepository districts)
        {
            Characters characters = session.Entity.Get<Characters>();

            if (!characters.TryGetValue(request.Id, out Entity entity))
                NetworkUtils.DropSession();

            characters.LastSelected = entity;
            session.SendCharacterSelect(districts);
        }

        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptionList()
        {
        }

        [Handler(ServerOpcode.CharacterChangeBackground, HandlerPermission.Authorized)]
        public static void ChangeBackground(GameSession session, ChangeBackgroundRequest request, BinTables binTable)
        {
            if (!binTable.CharacterBackgroundTable.TryGetValue(request.BackgroundId, out CharacterBackgroundTableEntity entity))
                NetworkUtils.DropSession();

            session.Entity.Set(entity);
            session.SendCharacterBackground();
        }
    }
}