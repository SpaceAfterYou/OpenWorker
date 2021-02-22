using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Requests.Friend;

namespace ow.Service.District.Network.Sync.Handlers
{
    public static class FriendHandler
    {
        [Handler(CategoryCommand.Friend, FriendCommand.Load)]
        public static void FriendLoadRequest(SyncSession session, FriendLoadRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.BlocklistLoad)]
        public static void FriendBlocklistLoadRequest(SyncSession session, FriendBlocklistLoadRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.Invite)]
        public static void FriendInviteRequest(SyncSession session, FriendInviteRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.InviteAccept)]
        public static void FriendAcceptRequest(SyncSession session, FriendInviteAcceptRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.Delete)]
        public static void FriendDeleteRequest(SyncSession session, FriendDeleteRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.BlocklistAdd)]
        public static void FriendBlocklistAddRequest(SyncSession session, FriendBlocklistAddRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.BlocklistDelete)]
        public static void FriendBlocklistDeleteRequest(SyncSession session, FriendBlocklistDeleteRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.Info)]
        public static void FriendInfoRequest(SyncSession session, FriendInfoRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.Find)]
        public static void FriendFindRequest(SyncSession session, FriendFindRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.RecruitList)]
        public static void FriendRecruitListRequest(SyncSession session, FriendRecruitListRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.RecruitAdd)]
        public static void FriendRecruitAddRequest(SyncSession session, FriendRecruitAddRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.RecruitDelete)]
        public static void FriendRecruitDeleteRequest(SyncSession session, FriendRecruitDeleteRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.RecruitInfo)]
        public static void FriendRecruitInfoRequest(SyncSession session, FriendRecruitInfoRequest request)
        {
        }

        [Handler(CategoryCommand.Friend, FriendCommand.RecommandList)]
        public static void FriendRecommandFriendListRequest(SyncSession session, FriendRecommandListRequest request)
        {
        }
    }
}