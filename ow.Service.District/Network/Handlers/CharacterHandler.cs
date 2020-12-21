using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;

namespace ow.Service.District.Network.Handlers
{
    internal static class CharacterHandler
    {
        //    [Handler(ServerOpcode.CharacterSpecialOptionUpdateList, HandlerPermission.Authorized)]
        //    internal static void UpdateSpecialOptions(Session session, CharacterSpecialOptionListUpdateRequest request) => session.Entity.Get<DimensionMemberEntity>()
        //        .SendCharacterSpecialOptionUpdateList(request);

        //    [Handler(ServerOpcode.OthersInfo, HandlerPermission.Authorized)]
        //    internal static void GetOthers(Session session, NpcRepository npcs) => session
        //        .SendNpcOtherInfos(npcs).Entity.Get<DimensionMemberEntity>()
        //        .SendCharacterOtherInfos();

        //    [Handler(ServerOpcode.CharacterInfo, HandlerPermission.Authorized)]
        //    internal static void GetInfo(Session session) => session
        //        .SendCharacterInfo()
        //        .SendCharacterStatsUpdate()
        //        .SendCharacterProfileInfo()
        //        .SendCharacterGestureLoad()
        //        .SendCharacterPostInfo();

        [Handler(ServerOpcode.CharacterToggleWeapon, HandlerPermission.Authorized)]
        internal static void ToggleWeapon(Session session, CharacterToggleWeaponRequest request) => session
            .SendAsync(request);
    }
}