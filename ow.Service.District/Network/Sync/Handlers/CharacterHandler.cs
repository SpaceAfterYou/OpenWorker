using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
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
            session.SendAsync(new NpcOthersInfosResponse()
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
            .SendAsync(new CharacterInfoResponse()
            {
                Character = ResponseHelper.GetCharacter(session),
                Place = ResponseHelper.GetPlace(session, _instance),
            })
            .SendAsync(new CharacterSkillInfoResponse()
            {
            })
            .SendAsync(new InfiniteTowerLoadInfoResponse()
            {
            })
            .SendAsync(new CharacterGestureLoadResponse()
            {
                Values = session.Gestures
            })
            .SendAsync(new CharacterProfileResponse()
            {
                About = session.Profile.About,
                Note = session.Profile.Note,
                Status = session.Profile.Status,
            })
            .SendAsync(new CharacterPostInfoResponse()
            {
                Values = Array.Empty<object>()
            })
            .SendAsync(new CharacterStatsUpdateResponse()
            {
                Character = session.Character.Id,
                Values = session.Stats.Select(s => new CharacterStatsUpdateResponse.Entity() { Id = s.Id, Value = s.Value })
            });

        [Handler(ServerOpcode.CharacterToggleWeapon, HandlerPermission.Authorized)]
        public static void ToggleWeapon(SyncSession session, CharacterToggleWeaponRequest request) => session
            .SendAsync(request);

        public CharacterHandler(Instance instance, NpcRepository npcs) =>
            (_instance, _npcs) = (instance, npcs);

        private readonly Instance _instance;
        private readonly NpcRepository _npcs;
    }
}