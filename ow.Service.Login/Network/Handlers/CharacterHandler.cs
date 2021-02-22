using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.IO.Network.Relay.World.Client.Protos.Requests;
using ow.Framework.Utils;
using ow.Service.Login.Game.Gate;
using ow.Service.Login.Game.Gate.Repository;
using ow.Service.Login.Network.Helpers;
using SoulCore;
using SoulCore.Data.Bin.Table.Entities;
using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.PacketSharedStructure;
using SoulCore.IO.Network.Requests;
using SoulCore.IO.Network.Requests.Character;
using SoulCore.IO.Network.Responses;
using SoulCore.Types;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ow.Service.Login.Network.Handlers
{
    public static class CharacterHandler
    {
        public static ValueTask<SyncSession> SendCharactersListAsync(SyncSession session) => session
            .SendAsync(new GateCharacterListResponse()
            {
                LastSelected = session.Characters.LastSelected?.Id ?? -1,
                InitializeTime = (ulong)session.Characters.InitializeTime.TotalSeconds,
                Characters = session.Characters.Values.Select(c => new CharacterPacketSharedStructure()
                {
                    Id = c.Id,
                    Name = c.Name,
                    Advancement = c.Advancement,
                    Class = c.Class,
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
                    SubWeaponItem = new(),
                    CostumesItems = new()
                    {
                        Battle = c.Storages.EquippedBattleFashion.Select(CreateCostumeInfo),
                        View = c.Storages.EquippedViewFashion.Select(CreateCostumeInfo)
                    }
                }).ToArray(),
            });

        private static CharacterPacketSharedStructure.CostumeInfos.CostumeInfo CreateCostumeInfo(CharacterList.Character.StoragesInfos.CostumeItems.CostumeItem item) =>
            new() { Color = item.Color, PrototypeId = item.PrototypeId };

        [Handler(CategoryCommand.Character, CharacterCommand.ChangeSlot)]
        public static async ValueTask OnSlotChangeAsync(SyncSession session, CharacterChangeSlotRequest request)
        {
            if (1 > request.Src.SlotId || request.Src.SlotId > Defines.ClassesCount)
            {
                NetworkUtils.Drop(session);
            }

            if (1 > request.Dst.SlotId || request.Dst.SlotId > Defines.ClassesCount)
            {
                NetworkUtils.Drop(session);
            }

            if (request.Src.SlotId == request.Dst.SlotId)
            {
                NetworkUtils.Drop(session);
            }

            await ChangeSlot(session, request).ConfigureAwait(false);
            await SendCharactersListAsync(session).ConfigureAwait(false);
        }

        private static async Task ChangeSlot(SyncSession session, CharacterChangeSlotRequest request)
        {
            CharacterList.Character?[]? characters = new[]
            {
                session.Characters.Values.FirstOrDefault(c => c.Slot == request.Src.SlotId),
                session.Characters.Values.FirstOrDefault(c => c.Slot == request.Dst.SlotId)
            };

            await using CharacterContext context = session.Server.DatabaseCharacterFactory.CreateDbContext();

            foreach (CharacterList.Character? character in characters)
            {
                if (character is null)
                {
                    continue;
                }

                CharacterModel? model = await context.Characters.FirstOrDefaultAsync(s => s.Id == character.Id).ConfigureAwait(true);
                if (model is null)
                {
                    continue;
                }

                model.Slot = character.Slot = request.Src.SlotId;

                context.Update(model);
            }

            await context.SaveChangesAsync().ConfigureAwait(true);
        }

        [Handler(CategoryCommand.Character, CharacterCommand.CreateReq)]
        public static async ValueTask OnCreateRequestAsync(SyncSession session, SCharacterCreateRequest request)
        {
            if (request.Character.Main.Name.Length > Defines.MaxCharacterNameLength)
            {
                return;
            }

            if (request.Character.Main.Name.Length < Defines.MinCharacterNameLength)
            {
                return;
            }

            if (request.Character.Main.Class == Class.None || !Enum.IsDefined(typeof(Class), request.Character.Main.Class))
            {
                NetworkUtils.Drop(session);
            }

            if (!session.Server.Tables.CustomizeHair.TryGetValue(request.Character.Main.Class, out CustomizeHairEntity? customizeHairEntity))
            {
                NetworkUtils.Drop(session);
            }

            if (request.Character.Main.Appearance.Hair.Style == 0 || !customizeHairEntity.Style.Contains(request.Character.Main.Appearance.Hair.Style))
            {
                NetworkUtils.Drop(session);
            }

            if (!session.Server.Tables.CustomizeEyes.TryGetValue(request.Character.Main.Class, out CustomizeEyesEntity? customizeEyesEntity))
            {
                NetworkUtils.Drop(session);
            }

            if (request.Character.Main.Appearance.EyesColor == 0 || !customizeEyesEntity.Color.Contains(request.Character.Main.Appearance.EyesColor))
            {
                NetworkUtils.Drop(session);
            }

            if (!session.Server.Tables.CustomizeSkin.TryGetValue(request.Character.Main.Class, out CustomizeSkinEntity? customizeSkinEntity))
            {
                NetworkUtils.Drop(session);
            }

            if (request.Character.Main.Appearance.SkinColor == 0 || !customizeSkinEntity.Color.Contains(request.Character.Main.Appearance.SkinColor))
            {
                NetworkUtils.Drop(session);
            }

            if (!session.Server.Tables.CharacterInfo.TryGetValue((ushort)(CharacterCreateHelper.CharacterOffset * (byte)request.Character.Main.Class), out CharacterInfoEntity? characterInfoEntity))
            {
                NetworkUtils.Drop(session);
            }

            if (request.Outfit == 0 || characterInfoEntity.DefaultCostumeIds.Contains(request.Outfit))
            {
                NetworkUtils.Drop(session);
            }

            if (!session.Server.Tables.ClassSelectInfo.TryGetValue(request.Character.Main.Class, out ClassSelectInfoEntity? classInfoEntity))
            {
                NetworkUtils.Drop(session);
            }

            PhotoItemEntity? photoItemEntity = session.Server.Tables.PhotoItem.Values.FirstOrDefault(c => c.Class == request.Character.Main.Class && c.PromotionInfo == 1);
            if (photoItemEntity is null)
            {
                NetworkUtils.Drop(session);
            }

            await AddCharacter(session, request, characterInfoEntity, photoItemEntity).ConfigureAwait(false);
            await SendCharactersListAsync(session).ConfigureAwait(false);
        }

        private static async Task AddCharacter(SyncSession session, SCharacterCreateRequest request, CharacterInfoEntity characterInfoEntity, PhotoItemEntity photoItemEntity)
        {
            await using CharacterContext context = session.Server.DatabaseCharacterFactory.CreateDbContext();

            if (await context.Characters.AnyAsync(c => c.Slot == request.Slot && c.AccountId == session.Account.Id).ConfigureAwait(true))
            {
                return;
            }

            if (await context.Characters.AnyAsync(c => c.Name == request.Character.Main.Name).ConfigureAwait(true))
            {
                return;
            }

            CharacterModel model = CharacterCreateHelper.CreateModel(session.Account, request, session.Server.Instance, characterInfoEntity, photoItemEntity);
            context.Add(model);

            await context.SaveChangesAsync().ConfigureAwait(true);

            session.Characters.LastSelected = await session.Server.CreateEmptyCharacaterAsync(model).ConfigureAwait(true);
            if (!session.Characters.TryAdd(model.Id, session.Characters.LastSelected))
            {
                return;
            }

            return;
        }

        [Handler(CategoryCommand.Character, CharacterCommand.DeleteReq)]
        public static async ValueTask OnDeleteRequestAsync(SyncSession session, CharacterDeleteRequest request)
        {
            if (!session.Characters.TryRemove(request.CharacterId, out CharacterList.Character _))
            {
                NetworkUtils.Drop(session);
            }

            if (session.Characters.LastSelected?.Id == request.CharacterId)
            {
                session.Characters.LastSelected = null;
            }

            if (request.CharacterId == session.Characters.Favorite?.Id)
            {
                session.Characters.Favorite = null;
            }

            await DeleteCharacter(session, request).ConfigureAwait(true);
            await SendCharactersListAsync(session).ConfigureAwait(true);
        }

        private static async Task DeleteCharacter(SyncSession session, CharacterDeleteRequest request)
        {
            await using CharacterContext context = session.Server.DatabaseCharacterFactory.CreateDbContext();

            CharacterModel? model = await context.Characters.FirstOrDefaultAsync(s => s.Id == request.CharacterId).ConfigureAwait(true);
            if (model is null)
            {
                return;
            }

            context.Remove(model);

            await context.SaveChangesAsync().ConfigureAwait(true);
        }

        [Handler(CategoryCommand.Character, CharacterCommand.ListReq)]
        public static async ValueTask OnListRequestAsync(SyncSession session) => await SendCharactersListAsync(session).ConfigureAwait(false);

        [Handler(CategoryCommand.Character, CharacterCommand.RepresentativeChange)]
        public static async ValueTask OnRepresentativeChangeAsync(SyncSession session, CharacterRepresentativeChangeRequest request)
        {
            if (session.Characters.TryGetValue(request.CharacterId, out CharacterList.Character? character))
            {
                await session.SendAsync(new GateCharacterMarkAsFavoriteResponse()
                {
                    AccountId = session.Account.Id,
                    CharacterId = character.Id,
                    CharacterName = character.Name,
                    PhotoId = character.Photo
                }).ConfigureAwait(false);
            }
        }

        [Handler(CategoryCommand.Character, CharacterCommand.SelectReq)]
        public static async ValueTask OnSelectAsync(SyncSession session, CharacterSelectRequest request)
        {
            if (!session.Characters.TryGetValue(request.CharacterId, out CharacterList.Character? character))
            {
                NetworkUtils.Drop(session);
            }

            if (!session.Server.Districts.TryGetValue(character.Place.District, out DistrictRepository.DistrictEntity? district))
            {
                NetworkUtils.Drop(session);
            }

            if (!district.Relay.Session.Reserve(new RWCSessionReserveRequest() { Character = character.Id }).Result)
            {
                await session.SendAsync(new EnterMapResponse { Result = 50011 }).ConfigureAwait(false);
            }

            session.Characters.LastSelected = character;

            await session.SendAsync(new SWorldCurrentDataResponse()).ConfigureAwait(false);
            await session.SendAsync(new EnterMapResponse
            {
                AccountId = session.Account.Id,
                CharacterId = character.Id,
                Pos = new()
                {
                    LocationId = character.Place.District,
                    Position = character.Place.Postion,
                    Rotation = character.Place.Rotation
                },
                EndPoint = new()
                {
                    Ip = district.Ip,
                    Port = district.Port
                }
            }).ConfigureAwait(false);
        }

        [Handler(CategoryCommand.Character, CharacterCommand.UpdateSpecialOptionList)]
        public static ValueTask OnUpdateSpecialOptionList() => ValueTask.CompletedTask;

        [Handler(CategoryCommand.Character, CharacterCommand.ChangeBackground)]
        public static async ValueTask OnChangeBackgroundAsync(SyncSession session, CharacterChangeBackgroundRequest request)
        {
            if (!session.Server.Tables.CharacterBackground.ContainsKey(request.BackgroundId))
            {
                NetworkUtils.Drop(session);
            }

            session.Background = request.BackgroundId;

            await session.SendAsync(new GateCharacterChangeBackgroundResponse()
            {
                AccountId = session.Account.Id,
                BackgroundId = session.Background
            }).ConfigureAwait(false);
        }
    }
}