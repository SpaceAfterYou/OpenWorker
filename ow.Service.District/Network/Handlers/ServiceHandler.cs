namespace ow.Service.District.Network.Handlers
{
    internal static class ServiceHandler
    {
        //public Channel Channel { get; set; }
        //public Character Character { get; init; }
        //public Profile Profile { get; init; }

        //[Handler(ServerOpcode.DistrictEnter, HandlerPermission.UnAuthorized)]
        //public static void Enter(Session session, EnterRequest request, IBoosterRepository boosters, IDayEventBoosterRepository dayEventBoosterRepository, IChannelRepository channels)
        //{
        //    var accountModel = await DistrictEnterHelper.GetAccountModel(request.AccountId, request.SessionKey);
        //    session.SetComponent(new Account(accountModel));

        //    var characterModel = await DistrictEnterHelper.GetCharacterModel(request.CharacterId, request.AccountId);

        //    var character = new Character(characterModel);
        //    session
        //        .SetComponent(character)
        //            .SetComponent(new Profile(characterModel))
        //            .SetComponent(new Stats())
        //            .SetComponent(new SpecialOptions())
        //            .SetComponent(new Gestures(characterModel));

        //    DistrictEnterHelper.CreateStorages(session, accountModel, characterModel);

        //    //Hub.SendSessionConnect(session, character);

        //    channels.JoinToFirstAvailable(session);

        //    session
        //        .ReTarget<AuthorizedGroupAttribute>()
        //        .SendCurrentDate()
        //        .SendWorldVersion()
        //        .SendDayEventBoosterList(dayEventBoosterRepository)
        //        .SendWorldEnter()
        //        .SendAddBoosters(boosters)
        //        //eSUB_CMD_POST_ACCOUNT_RECV
        //        //.SendAttendanceRewardLoad()
        //        //.SendAttendancePlayTimeInit()
        //        //receive_eSUB_CMD_EXCHANGE_INTEREST_LIST
        //        //receive_eSUB_CMD_EVENT_ROULETTE_MY_INFO
        //        //receive_eSUB_CMD_ITEM_AKASHIC_GETINFO_LOAD
        //        //.SendInfiniteTowerLoadInfo()
        //        //receive_eSUB_CMD_ENTER_MAZE_LIMIT_COUNT_RESET
        //        //.SendAttendanceReward()
        //        //.SendAttendanceContinueReward()
        //        // receive_eSUB_CMD_FRIEND_LOAD
        //        // receive_eSUB_CMD_FRIEND_LOAD_BLOCKLIST
        //        .SendCharacterDbLoadSync();
        //}

        //[Handler(ServerOpcode.Heartbeat, HandlerPermission.Authorized)]
        //public static void Heartbeat(Session session, HeartbeatRequest request) =>
        //session.SendServerHeartbeat(request);

        //[Handler(ServerOpcode.LogOut, HandlerPermission.Authorized)]
        //public static void LogOut(Session session, LogoutRequest request, GateConnection connection)
        //{
        //    var account = session.GetComponent<Account>();
        //    if (account.Id != request.AccountId)
        //    {
        //        return;
        //    }

        //    var character = session.GetComponent<Character>();
        //    if (character.Id != request.CharacterId)
        //    {
        //        return;
        //    }

        //    session.SendServerLogOut(account, character, connection);
        //}
    }
}