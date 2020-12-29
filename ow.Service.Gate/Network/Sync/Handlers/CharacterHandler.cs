using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.IO.Network.Sync.Responses.Shared;
using ow.Framework.Utils;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Repository;
using ow.Service.Gate.Network.Helpers;
using System;
using System.Linq;

using static ow.Framework.IO.Network.Sync.Responses.Shared.CharacterShared.EquippedItemsInfo;

namespace ow.Service.Gate.Network.Handlers
{
    public sealed class CharacterHandler
    {
        public static void SendCharactersListAsync(Session session) =>
            session.SendAsync(new GateCharacterListResponse()
            {
                LastSelected = session.Characters.LastSelected?.Id ?? -1,
                InitializeTime = (ulong)session.Characters.InitializeTime.TotalSeconds,
                Characters = session.Characters.Values.Select(c => new CharacterShared()
                {
                    Id = c.Id,
                    Name = c.Name,
                    Advancement = c.Advancement,
                    Hero = c.Hero,
                    Slot = c.Slot,
                    Level = c.Level,
                    Photo = c.Photo,
                    Stats = new()
                    {
                        AttackSpeed = 1.0f,
                        MoveSpeed = 1.0f,
                    },
                    Appearance = new()
                    {
                        EquippedEyeColor = c.Appearance.EquippedEyeColor,
                        EquippedHair = new()
                        {
                            Color = c.Appearance.EquippedHair.Color,
                            Style = c.Appearance.EquippedHair.Style,
                        },
                        Hair = new()
                        {
                            Style = c.Appearance.Hair.Style,
                            Color = c.Appearance.Hair.Color,
                        },
                        EquippedSkinColor = c.Appearance.EquippedSkinColor,
                        EyeColor = c.Appearance.EyeColor,
                        SkinColor = c.Appearance.SkinColor,
                    },
                    WeaponItem = new()
                    {
                        PrototypeId = c.Storages.EquippedGearWeapon.PrototypeId,
                        UpgradeLevel = c.Storages.EquippedGearWeapon.UpgradeLevel,
                    },
                    EquippedItems = new()
                    {
                        Battle = c.Storages.EquippedBattleFashion.Select(s => s is null ? null : new FashionItemInfo() { Color = s.Color, PrototypeId = s.PrototypeId }),
                        View = c.Storages.EquippedViewFashion.Select(s => s is null ? null : new FashionItemInfo() { Color = s.Color, PrototypeId = s.PrototypeId }),
                    }
                }).ToArray(),
            });

        [Handler(ServerOpcode.CharacterChangeSlot, HandlerPermission.Authorized)]
        public void ChangeSlot(Session session, CharacterChangeSlotRequest request)
        {
            if (1 > request.FirstSlot || request.FirstSlot > Defines.CharactersSlotsCount)
                NetworkUtils.DropSession();

            if (1 > request.SecondSlot || request.SecondSlot > Defines.CharactersSlotsCount)
                NetworkUtils.DropSession();

            if (request.FirstSlot == request.SecondSlot)
                NetworkUtils.DropSession();

            Characters.Entity? first = session.Characters.Values.FirstOrDefault(c => c.Slot == request.FirstSlot);
            Characters.Entity? second = session.Characters.Values.FirstOrDefault(c => c.Slot == request.SecondSlot);

            using CharacterContext context = _characterFactory();

            if (first is not null)
            {
                first.Slot = request.FirstSlot;
                context.Update(new { first.Id, first.Slot });
            }

            if (second is not null)
            {
                second.Slot = request.SecondSlot;
                context.Update(new { second.Id, second.Slot });
            }

            context.SaveChanges();

            SendCharactersListAsync(session);
        }

        [Handler(ServerOpcode.CharacterCreate, HandlerPermission.Authorized)]
        public void Create(Session session, CharacterCreateRequest request)
        {
            if (request.Character.Main.Name.Length > Defines.MaxCharacterNameLength)
                return;

            if (request.Character.Main.Name.Length < Defines.MinCharacterNameLength)
                return;

            CharacterCreateHelper.ValidateHero(request);
            CharacterCreateHelper.ValidateHair(request, _tables);
            CharacterCreateHelper.ValidateEyes(request, _tables);
            CharacterCreateHelper.ValidateSkin(request, _tables);
            CharacterCreateHelper.ValidateOutfit(request, _tables);

            using CharacterContext context = _characterFactory();

            if (context.Characters.Any(c => c.Slot == request.Slot && c.AccountId == session.Account.Id))
                NetworkUtils.DropSession();

            if (context.Characters.Any(c => c.Name == request.Character.Main.Name))
                return;

            if (!_tables.ClassSelectInfo.TryGetValue(request.Character.Main.Hero, out ClassSelectInfoTableEntity? classInfo))
                NetworkUtils.DropSession();

            /// [ TODO ] Add default items to inventory

            CharacterModel model = CharacterCreateHelper.CreateModel(session.Account, request, _gate, _tables);
            context.UseAndSave(c => c.Add(model));

            if (!session.Characters.TryAdd(model.Id, session.Characters.LastSelected = CreateEmptyCharacater(model)))
                NetworkUtils.DropSession();

            SendCharactersListAsync(session);
        }

        [Handler(ServerOpcode.CharacterDelete, HandlerPermission.Authorized)]
        public void Delete(Session session, CharacterDeleteRequest request)
        {
            if (!session.Characters.Remove(request.Id, out Characters.Entity _))
                NetworkUtils.DropSession();

            if (session.Characters.LastSelected?.Id == request.Id)
                session.Characters.LastSelected = null;

            if (request.Id == session.Characters.Favorite?.Id)
                session.Characters.Favorite = null;

            using CharacterContext context = _characterFactory();
            context.UseAndSave(c => c.Remove<CharacterModel>(new() { Id = request.Id }));

            SendCharactersListAsync(session);
        }

        [Handler(ServerOpcode.CharacterList, HandlerPermission.Authorized)]
        public static void GetList(Session session) => SendCharactersListAsync(session);

        [Handler(ServerOpcode.CharacterMarkFavorite, HandlerPermission.Authorized)]
        public static void MarkFavorite(Session session, CharacterMarkFavoriteRequest request)
        {
            if (!session.Characters.TryGetValue(request.Id, out Characters.Entity? character))
                NetworkUtils.DropSession();

            session.SendAsync(new GateCharacterMarkAsFavoriteResponse()
            {
                AccountId = session.Account.Id,
                CharacterId = character!.Id,
                CharacterName = character!.Name,
                PhotoId = character!.Photo
            });
        }

        [Handler(ServerOpcode.CharacterSelect, HandlerPermission.Authorized)]
        public void Select(Session session, CharacterSelectRequest request)
        {
            if (!session.Characters.TryGetValue(request.Id, out Characters.Entity? character))
                NetworkUtils.DropSession();

            if (!_districts.TryGetValue(character!.Place.District, out DistrictRepository.Entity? district))
                NetworkUtils.DropSession();

            session.Characters.LastSelected = character;

            session.SendAsync(new ServiceCurrentDataResponse());
            session.SendAsync(new GateCharacterSelectResponse()
            {
                AccountId = session.Account.Id,
                CharacterId = character!.Id,
                Place = new()
                {
                    Location = character.Place.District,
                    Position = character.Place.Postion,
                    Rotation = character.Place.Rotation
                },
                EndPoint = new()
                {
                    Ip = district!.Ip,
                    Port = district!.Port
                }
            });
        }

        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptionList()
        {
        }

        [Handler(ServerOpcode.CharacterChangeBackground, HandlerPermission.Authorized)]
        public void ChangeBackground(Session session, CharacterChangeBackgroundRequest request)
        {
            if (!_tables.CharacterBackground.ContainsKey(request.BackgroundId))
                NetworkUtils.DropSession();

            session.Background = request.BackgroundId;

            session.SendAsync(new GateCharacterChangeBackgroundResponse()
            {
                AccountId = session.Account.Id,
                BackgroundId = session.Background
            });
        }

        public CharacterHandler(Func<ItemContext> itemFactory, Func<CharacterContext> characterFactory, DistrictRepository districts, GateInstance gate, BinTables tables)
        {
            _itemFactory = itemFactory;
            _characterFactory = characterFactory;
            _districts = districts;
            _gate = gate;
            _tables = tables;
        }

        private Characters.Entity CreateEmptyCharacater(CharacterModel model)
        {
            using ItemContext itemContext = _itemFactory();
            return new(model, _tables, itemContext);
        }

        private readonly Func<ItemContext> _itemFactory;
        private readonly Func<CharacterContext> _characterFactory;
        private readonly DistrictRepository _districts;
        private readonly GateInstance _gate;
        private readonly BinTables _tables;
    }
}