using ow.Framework;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Framework.IO.Network.Responses;
using ow.Framework.IO.Network.Responses.Shared;
using ow.Framework.Utils;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Repository;
using ow.Service.Gate.Network.Helpers;
using System.Linq;

using static ow.Framework.IO.Network.Responses.Shared.CharacterShared.EquippedItemsInfo;

namespace ow.Service.Gate.Network.Handlers
{
    internal static class CharacterHandler
    {
        private static void SendCharactersListAsync(Session session) =>
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
                        Battle = c.Storages.EquippedBattleFashion.Select(s => new ItemInfo() { Color = s.Color, PrototypeId = s.PrototypeId }),
                        View = c.Storages.EquippedViewFashion.Select(s => new ItemInfo() { Color = s.Color, PrototypeId = s.PrototypeId }),
                    }
                }).ToArray(),
            });

        [Handler(ServerOpcode.CharacterChangeSlot, HandlerPermission.Authorized)]
        public static void ChangeSlot(Session session, CharacterChangeSlotRequest request)
        {
            if (1 > request.FirstSlot || request.FirstSlot > Defines.CharactersSlotsCount)
                NetworkUtils.DropSession();

            if (1 > request.SecondSlot || request.SecondSlot > Defines.CharactersSlotsCount)
                NetworkUtils.DropSession();

            if (request.FirstSlot == request.SecondSlot)
                NetworkUtils.DropSession();

            Character? first = session.Characters.Values.FirstOrDefault(c => c.Slot == request.FirstSlot);
            Character? second = session.Characters.Values.FirstOrDefault(c => c.Slot == request.SecondSlot);

            using CharacterContext context = new();

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
        public static void Create(Session session, CharacterCreateRequest request, GateInstance gate, BinTables tables)
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

            if (context.Characters.Any(c => c.Slot == request.Slot && c.AccountId == session.Account.Id))
                NetworkUtils.DropSession();

            if (context.Characters.Any(c => c.Name == request.Character.Main.Name))
                return;

            if (!tables.ClassSelectInfo.TryGetValue(request.Character.Main.Hero, out ClassSelectInfoTableEntity? classInfo))
                NetworkUtils.DropSession();

            /// [ TODO ] Add default items to inventory

            CharacterModel model = CharacterCreateHelper.CreateModel(session.Account, request, gate, tables);
            context.UseAndSave(c => c.Add(model));

            if (!session.Characters.TryAdd(model.Id, session.Characters.LastSelected = new(model, tables)))
                NetworkUtils.DropSession();

            SendCharactersListAsync(session);
        }

        [Handler(ServerOpcode.CharacterDelete, HandlerPermission.Authorized)]
        public static void Delete(Session session, CharacterDeleteRequest request)
        {
            if (!session.Characters.Remove(request.Id, out Character _))
                NetworkUtils.DropSession();

            if (session.Characters.LastSelected?.Id == request.Id)
                session.Characters.LastSelected = null;

            if (request.Id == session.Characters.Favorite?.Id)
                session.Characters.Favorite = null;

            using CharacterContext context = new();
            context.UseAndSave(c => c.Remove<CharacterModel>(new() { Id = request.Id }));

            SendCharactersListAsync(session);
        }

        [Handler(ServerOpcode.CharacterList, HandlerPermission.Authorized)]
        public static void GetList(Session session) => SendCharactersListAsync(session);

        [Handler(ServerOpcode.CharacterMarkFavorite, HandlerPermission.Authorized)]
        public static void MarkFavorite(Session session, CharacterMarkFavoriteRequest request)
        {
            if (!session.Characters.TryGetValue(request.Id, out Character? character))
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
        public static void Select(Session session, CharacterSelectRequest request, DistrictRepository districts)
        {
            if (!session.Characters.TryGetValue(request.Id, out Character? character))
                NetworkUtils.DropSession();

            if (!districts.TryGetValue(character!.Place.District.Id, out DistrictRepository.Entity? district))
                NetworkUtils.DropSession();

            session.Characters.LastSelected = character;

            session.SendAsync(new ServiceCurrentDataResponse());
            session.SendAsync(new GateCharacterSelectResponse()
            {
                AccountId = session.Account.Id,
                CharacterId = character!.Id,
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
        public static void ChangeBackground(Session session, CharacterChangeBackgroundRequest request, BinTables binTable)
        {
            if (!binTable.CharacterBackground.ContainsKey(request.BackgroundId))
                NetworkUtils.DropSession();

            session.Background = request.BackgroundId;

            session.SendAsync(new GateCharacterChangeBackgroundResponse()
            {
                AccountId = session.Account.Id,
                BackgroundId = session.Background
            });
        }
    }
}