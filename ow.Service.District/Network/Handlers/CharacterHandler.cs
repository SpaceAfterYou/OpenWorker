using ow.Framework.Game.Entities;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Character;
using ow.Service.District.Game;

namespace ow.Service.District.Network.Handlers
{
    internal static class CharacterHandler
    {
        [Handler(ServerOpcode.OthersInfo, HandlerPermission.Authorized)]
        public static void GetOthers(GameSession session, CachedNpcs npcs)
        {
            session.Entity.Get<DimensionMemberEntity>().SendCharacterOtherInfos();
            session.SendNpcOtherInfos(npcs);
        }

        [Handler(ServerOpcode.CharacterInfo, HandlerPermission.Authorized)]
        public static void GetInfo(GameSession session, CachedNpcs npcs) => session
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