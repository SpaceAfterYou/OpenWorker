using Microsoft.EntityFrameworkCore;
using SoulCore;
using SoulCore.Database.Characters;
using SoulCore.Database.Storages;
using SoulCore.Game.Datas.Bin.Table.Entities;
using SoulCore.IO.Network.Relay.World.Client.Protos.Requests;
using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using SoulCore.IO.Network.Sync.Responses.Shared;
using SoulCore.Utils;
using ow.Service.World.Game.Gate;
using ow.Service.World.Game.Gate.Repository;
using ow.Service.World.Network.Gate.Sync.Helpers;
using System.Linq;

using static SoulCore.IO.Network.Sync.Responses.Shared.CharacterShared.EquippedItemsInfo;
using static ow.Service.World.Game.Gate.Characters;
using static ow.Service.World.Game.Gate.Repository.DistrictRepository;

namespace ow.Service.World.Network.Gate.Sync.Handlers
{
    public sealed class CharacterHandler
    {
        public static void SendCharactersListAsync(SyncSession session) =>
            session.SendDeferred(new GateCharacterListResponse()
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
                        EquippedEyeColor = c.Appearance.EquippedEyesColor,
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
                        EyeColor = c.Appearance.EyesColor,
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
        public void ChangeSlot(SyncSession session, SCharacterChangeSlotRequest request)
        {
            if (1 > request.FirstSlot || request.FirstSlot > Defines.CharactersSlotsCount)
                NetworkUtils.DropBadAction();

            if (1 > request.SecondSlot || request.SecondSlot > Defines.CharactersSlotsCount)
                NetworkUtils.DropBadAction();

            if (request.FirstSlot == request.SecondSlot)
                NetworkUtils.DropBadAction();

            CEntity? first = session.Characters.Values.FirstOrDefault(c => c.Slot == request.FirstSlot);
            CEntity? second = session.Characters.Values.FirstOrDefault(c => c.Slot == request.SecondSlot);

            using CharacterContext context = _characterFactory.CreateDbContext();

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
        public void Create(SyncSession session, SCharacterCreateRequest request)
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

            using CharacterContext context = _characterFactory.CreateDbContext();

            if (context.Characters.Any(c => c.Slot == request.Slot && c.AccountId == session.Account.Id))
                NetworkUtils.DropBadAction();

            if (context.Characters.Any(c => c.Name == request.Character.Main.Name))
                return;

            if (!_tables.ClassSelectInfo.TryGetValue(request.Character.Main.Hero, out ClassSelectInfoTableEntity? classInfo))
                NetworkUtils.DropBadAction();

            if (!_tables.CharacterInfo.TryGetValue((ushort)((int)request.Character.Main.Hero * 1000), out CharacterInfoTableEntity? characterInfo))
                NetworkUtils.DropBadAction();

            /// [ TODO ] Add default items to inventory

            CharacterModel model = CharacterCreateHelper.CreateModel(session.Account, request, _gate, _tables);
            context.UseAndSave(c => c.Add(model));

            session.Characters.LastSelected = CreateEmptyCharacater(model);
            if (!session.Characters.TryAdd(model.Id, session.Characters.LastSelected))
                NetworkUtils.DropBadAction();

            SendCharactersListAsync(session);
        }

        [Handler(ServerOpcode.CharacterDelete, HandlerPermission.Authorized)]
        public void Delete(SyncSession session, SCharacterDeleteRequest request)
        {
            if (!session.Characters.Remove(request.Id, out CEntity _))
                NetworkUtils.DropBadAction();

            if (session.Characters.LastSelected?.Id == request.Id)
                session.Characters.LastSelected = null;

            if (request.Id == session.Characters.Favorite?.Id)
                session.Characters.Favorite = null;

            using CharacterContext context = _characterFactory.CreateDbContext();
            context.UseAndSave(c => c.Remove<CharacterModel>(new() { Id = request.Id }));

            SendCharactersListAsync(session);
        }

        [Handler(ServerOpcode.CharacterList, HandlerPermission.Authorized)]
        public static void GetList(SyncSession session) => SendCharactersListAsync(session);

        [Handler(ServerOpcode.CharacterMarkFavorite, HandlerPermission.Authorized)]
        public static void MarkFavorite(SyncSession session, SCharacterMarkFavoriteRequest request)
        {
            if (!session.Characters.TryGetValue(request.Id, out CEntity? character))
                NetworkUtils.DropBadAction();

            session.SendDeferred(new GateCharacterMarkAsFavoriteResponse()
            {
                AccountId = session.Account.Id,
                CharacterId = character!.Id,
                CharacterName = character!.Name,
                PhotoId = character!.Photo
            });
        }

        [Handler(ServerOpcode.CharacterSelect, HandlerPermission.Authorized)]
        public void Select(SyncSession session, SCharacterSelectRequest request)
        {
            if (!session.Characters.TryGetValue(request.Id, out CEntity? character))
                NetworkUtils.DropBadAction();

            if (!_districts.TryGetValue(character!.Place.District, out DistrictEntity? district))
                NetworkUtils.DropBadAction();

            if (!district!.Relay.Session.Reserve(new RWCSessionReserveRequest() { Character = character.Id }).Result)
            {
                session.SendAsync(new SGateCharacterSelectResponse());
                return;
            }

            session.Characters.LastSelected = character;

            session.SendDeferred(new SWorldCurrentDataResponse());
            session.SendAsync(new SGateCharacterSelectResponse
            {
                AccountId = session.Account.Id,
                CharacterId = character.Id,
                Pos = new()
                {
                    LocationId = character.Place.District,
                    Pos = character.Place.Postion,
                    Rot = character.Place.Rotation
                },
                EndPoint = new()
                {
                    Ip = district.Ip,
                    Port = district.Port
                }
            });
        }

        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptionList()
        {
        }

        [Handler(ServerOpcode.CharacterChangeBackground, HandlerPermission.Authorized)]
        public void ChangeBackground(SyncSession session, SCharacterChangeBackgroundRequest request)
        {
            if (!_tables.CharacterBackground.ContainsKey(request.BackgroundId))
                NetworkUtils.DropBadAction();

            session.Background = request.BackgroundId;

            session.SendDeferred(new GateCharacterChangeBackgroundResponse()
            {
                AccountId = session.Account.Id,
                BackgroundId = session.Background
            });
        }

        public CharacterHandler(IDbContextFactory<ItemContext> itemFactory, IDbContextFactory<CharacterContext> characterFactory, DistrictRepository districts, Instance gate, BinTables tables)
        {
            _itemFactory = itemFactory;
            _characterFactory = characterFactory;
            _districts = districts;
            _gate = gate;
            _tables = tables;
        }

        private CEntity CreateEmptyCharacater(CharacterModel model)
        {
            using ItemContext itemContext = _itemFactory.CreateDbContext();
            return new(model, itemContext);
        }

        private readonly IDbContextFactory<ItemContext> _itemFactory;
        private readonly IDbContextFactory<CharacterContext> _characterFactory;
        private readonly DistrictRepository _districts;
        private readonly Instance _gate;
        private readonly BinTables _tables;
    }
}
