using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.District.Game;
using ow.Service.District.Game.Helpers;
using System.Linq;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class CharacterHandler
    {
        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptions(Session session, CharacterSpecialOptionListUpdateRequest request) => session.Dimension!
            .SendAsync(request);

        [Handler(ServerOpcode.OthersInfo, HandlerPermission.Authorized)]
        public void GetOthers(Session session)
        {
            session.SendAsync(new NpcOthersInfosResponse()
            {
                Values = _npcs.Select(s => new NpcOthersInfosResponse.Entity()
                {
                    Id = s.Id,
                    NpcId = s.MobId,
                    Position = s.Position,
                    Rotation = s.Rotation,
                    Waypoint = s.Waypoint
                })
            });

            session.Dimension!.SendOtherCharactersAsync(_instance);
        }

        [Handler(ServerOpcode.CharacterInfo, HandlerPermission.Authorized)]
        public void GetInfo(Session session) => session
            .SendAsync(new CharacterInfoResponse()
            {
                Character = ResponseHelper.GetCharacter(session),
                Place = ResponseHelper.GetPlace(session, _instance),
            });

        //.SendCharacterStatsUpdate()
        //.SendCharacterProfileInfo()
        //.SendCharacterGestureLoad()
        //.SendCharacterPostInfo();

        [Handler(ServerOpcode.CharacterToggleWeapon, HandlerPermission.Authorized)]
        public static void ToggleWeapon(Session session, CharacterToggleWeaponRequest request) => session
            .SendAsync(request);

        public CharacterHandler(Instance instance, NpcRepository npcs) =>
            (_instance, _npcs) = (instance, npcs);

        private readonly Instance _instance;
        private readonly NpcRepository _npcs;
    }
}