using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Permissions;
using SoulCore.IO.Network.Requests.League;

namespace ow.Service.District.Network.Sync.Handlers
{
    public static class LeagueHandler
    {
        [Handler(CategoryCommand.League, LeagueCommand.Create)]
        public static void OnCreate(SyncSession session, LeagueCreateRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Delete)]
        public static void OnDelete(SyncSession session, LeagueDeleteRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.List)]
        public static void OnList(SyncSession session, LeagueListRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Applicant)]
        public static void OnApplicant(SyncSession session, LeagueApplicantRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Info)]
        public static void OnInfo(SyncSession session, LeagueInfoRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Board)]
        public static void OnBoard(SyncSession session, LeagueBoardRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Withdraw)]
        public static void OnWithDraw(SyncSession session, LeagueWithdrawRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Delegate)]
        public static void OnDelegate(SyncSession session, LeagueDelegateRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Kick)]
        public static void OnKick(SyncSession session, LeagueKickRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Invite)]
        public static void OnInvite(SyncSession session, LeagueInviteRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.InviteAccept)]
        public static void OnInviteAccept(SyncSession session, LeagueInviteAcceptRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.InviteReject)]
        public static void OnInviteReject(SyncSession session, LeagueInviteRejectRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.ApplicantAccept)]
        public static void OnApplicantAccept(SyncSession session, LeagueApplicantAcceptRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.ApplicantReject)]
        public static void OnApplicantReject(SyncSession session, LeagueApplicantRejectRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.Search)]
        public static void OnSearch(SyncSession session, LeagueSearchRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.OverlapName)]
        public static void OnOverlapName(SyncSession session, LeagueOverlapNameRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.NoticeChange)]
        public static void OnNoticeChange(SyncSession session, LeagueNoticeChangeRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.NameChange)]
        public static void OnNameChange(SyncSession session, LeagueNameChangeRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.CardChange)]
        public static void OnCardChange(SyncSession session, LeagueCardChangeRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.AuthChange)]
        public static void OnAuthChange(SyncSession session, LeagueAuthChangeRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.PositionNameChange)]
        public static void OnPositionNameChange(SyncSession session, LeaguePositionNameChangeRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.MemberPositionChange)]
        public static void OnMemberPositionChange(SyncSession session, LeagueMemberPositionChangeRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.OpenOrNot)]
        public static void OnOpenOrNot(SyncSession session, LeagueOpenOrNotRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.RecruitNotice)]
        public static void OnRecruitNotice(SyncSession session, LeagueRecruitNoticeRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.SkillLearn)]
        public static void OnSkillLearn(SyncSession session, LeagueSkillLearnRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.InventoryMove)]
        public static void OnInventoryMove(SyncSession session, LeagueInventoryMoveRequest request)
        {
        }

        [Handler(CategoryCommand.League, LeagueCommand.InventoryInfo)]
        public static void OnInventoryInfo(SyncSession session, LeagueInventoryInfoRequest request)
        {
        }
    }
}
