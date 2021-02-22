using ow.Service.District.Game;
using ow.Service.District.Game.Helpers;
using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Requests.Character;
using SoulCore.IO.Network.Responses;
using System;
using System.Linq;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class CharacterHandler
    {
        [Handler(CategoryCommand.Character, CharacterCommand.UpdateSpecialOptionList)]
        public static void UpdateSpecialOptions(SyncSession session, CharacterUpdateSpecialOptionListRequest request) => session.Channel
            .SendAsync(request);

        [Handler(CategoryCommand.Character, CharacterCommand.OtherInfo)]
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

            session.Channel.SendOtherCharactersDeferred(_instance);
        }

        [Handler(CategoryCommand.Character, CharacterCommand.InfoReq)]
        public void GetInfo(SyncSession session) => session
            .SendAsync(new CharacterInfoResponse()
            {
                Character = ResponseHelper.GetCharacterExPacket(session),
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
                Values = session.Stats.Select(s => new CharacterStatsUpdateResponse.CSUREntity() { Id = s.Id, Value = s.Value })
            });

        [Handler(CategoryCommand.Character, CharacterCommand.CharacterToggleWeapon)]
        public static void ToggleWeapon(SyncSession session, CharacterToggleWeaponRequest request) => session.Channel
            !.BroadcastDeferred(request);

        public CharacterHandler(Instance instance, NpcRepository npcs) =>
            (_instance, _npcs) = (instance, npcs);

        private readonly Instance _instance;
        private readonly NpcRepository _npcs;
    }
}