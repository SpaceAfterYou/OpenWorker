using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using ow.Service.District.Game;
using ow.Service.District.Game.Helpers;
using System;
using System.Linq;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class CharacterHandler
    {
        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptions(SyncSession session, CharacterSpecialOptionListUpdateRequest request) => session.Channel!
            .SendAsync(request);

        [Handler(ServerOpcode.OthersInfo, HandlerPermission.Authorized)]
        public void GetOthers(SyncSession session)
        {
            session.SendDeferred(new NpcOthersInfosResponse()
            {
                Values = _npcs.Select(s => new NpcOthersInfosResponse.Entity()
                {
                    Id = s.Id,
                    CreatureId = s.CreatureId,
                    Position = s.Position,
                    Rotation = s.Rotation,
                    Waypoint = s.Waypoint
                })
            });

            session.Channel!.SendOtherCharactersAsync(_instance);
        }

        [Handler(ServerOpcode.CharacterInfo, HandlerPermission.Authorized)]
        public void GetInfo(SyncSession session) => session
            .SendDeferred(new CharacterInfoResponse()
            {
                Character = ResponseHelper.GetCharacter(session),
                Place = ResponseHelper.GetPlace(session, _instance),
            })
            .SendDeferred(new CharacterSkillInfoResponse()
            {
            })
            .SendDeferred(new InfiniteTowerLoadInfoResponse()
            {
            })
            .SendDeferred(new CharacterGestureLoadResponse()
            {
                Values = session.Gestures
            })
            .SendDeferred(new CharacterProfileResponse()
            {
                About = session.Profile.About,
                Note = session.Profile.Note,
                Status = session.Profile.Status,
            })
            .SendDeferred(new CharacterPostInfoResponse()
            {
                Values = Array.Empty<object>()
            })
            .SendDeferred(new CharacterStatsUpdateResponse()
            {
                Character = session.Character.Id,
                Values = session.Stats.Select(s => new CharacterStatsUpdateResponse.CSUREntity() { Id = s.Id, Value = s.Value })
            });

        [Handler(ServerOpcode.CharacterToggleWeapon, HandlerPermission.Authorized)]
        public static void ToggleWeapon(SyncSession session, CharacterToggleWeaponRequest request) => session.Channel
            !.BroadcastDeferred(request);

        public CharacterHandler(Instance instance, NpcRepository npcs) =>
            (_instance, _npcs) = (instance, npcs);

        private readonly Instance _instance;
        private readonly NpcRepository _npcs;
    }
}
