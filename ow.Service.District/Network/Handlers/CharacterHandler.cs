using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Framework.IO.Network.Responses;
using ow.Service.District.Game;
using ow.Service.District.Game.Helpers;

namespace ow.Service.District.Network.Handlers
{
    internal static class CharacterHandler
    {
        //    [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        //    public static void UpdateSpecialOptions(Session session, CharacterSpecialOptionListUpdateRequest request) => session.Entity.Get<DimensionMemberEntity>()
        //        .SendCharacterSpecialOptionUpdateList(request);

        //    [Handler(ServerOpcode.OthersInfo, HandlerPermission.Authorized)]
        //    public static void GetOthers(Session session, NpcRepository npcs) => session
        //        .SendNpcOtherInfos(npcs).Entity.Get<DimensionMemberEntity>()
        //        .SendCharacterOtherInfos();

        [Handler(ServerOpcode.CharacterInfo, HandlerPermission.Authorized)]
        public static void GetInfo(Session session, Instance instance) => session
            .SendAsync(new CharacterInfoResponse()
            {
                Character = ResponseHelper.GetCharacter(session),
                Place = ResponseHelper.GetPlace(session, instance)
            });

        //.SendCharacterStatsUpdate()
        //.SendCharacterProfileInfo()
        //.SendCharacterGestureLoad()
        //.SendCharacterPostInfo();

        [Handler(ServerOpcode.CharacterToggleWeapon, HandlerPermission.Authorized)]
        public static void ToggleWeapon(Session session, CharacterToggleWeaponRequest request) => session
            .SendAsync(request);
    }
}