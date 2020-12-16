using ow.Framework.Game.Entities;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.SpecialOption;
using ow.Service.District.Game;

namespace ow.Service.District.Network.Handlers
{
    internal static class CharacterHandler
    {
        [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        public static void UpdateSpecialOptions(GameSession session, UpdateListRequest request) => session.Entity.Get<DimensionMemberEntity>()
            .SendCharacterSpecialOptionUpdateList(request);

        [Handler(ServerOpcode.OthersInfo, HandlerPermission.Authorized)]
        public static void GetOthers(GameSession session, CachedNpcs npcs) => session
            .SendNpcOtherInfos(npcs).Entity.Get<DimensionMemberEntity>()
            .SendCharacterOtherInfos();

        [Handler(ServerOpcode.CharacterInfo, HandlerPermission.Authorized)]
        public static void GetInfo(GameSession session) => session
            .SendCharacterInfo()
            .SendCharacterStatsUpdate()
            .SendCharacterProfileInfo()
            .SendCharacterGestureLoad()
            .SendCharacterPostInfo();

        [Handler(ServerOpcode.CharacterToggleWeapon, HandlerPermission.Authorized)]
        public static void ToggleWeapon(GameSession session, ToggleWeaponRequest request) => session
            .SendCharacterToggleWeapon(request);
    }
}