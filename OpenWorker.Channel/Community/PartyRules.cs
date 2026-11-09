namespace OpenWorker.Channel.Community;

/// <summary>
/// The sizes and times of parties, the recruitment board, the awaiters and the auto matching.
/// </summary>
public static class PartyRules
{
    /// <summary>
    /// A party holds the master and three more.
    /// </summary>
    public const int MaxMembers = 4;

    /// <summary>
    /// A party of one is no party: the second to last who leaves ends it.
    /// </summary>
    public const int MinMembers = 2;

    /// <summary>
    /// The others in a recruitment's list of applicants; the client always reads this many places.
    /// </summary>
    public const int MaxApplicants = 10;

    /// <summary>
    /// How many recruitments one person may have applied to at once; the client shows what is left of it.
    /// </summary>
    public const int MaxApplications = 5;

    /// <summary>
    /// The longest message of a recruitment, in characters.
    /// </summary>
    public const int RecruitMessageLength = 20;

    /// <summary>
    /// An auto matching group takes those whose level is this close to its average.
    /// </summary>
    public const int MatchingLevelGap = 5;

    public static TimeSpan InviteLifetime { get; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A party nobody of which is online is forgotten after this; every server renews the parties of its own people.
    /// </summary>
    public static TimeSpan PartyLifetime { get; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// A member who is not online anywhere for this long leaves the party: a move to another server takes less, a crash or
    /// a logout more. It is the lifetime of the presence of a person.
    /// </summary>
    public static TimeSpan MemberGrace { get; } = CommunityNetwork.PresenceLifetime;

    public static TimeSpan RecruitLifetime { get; } = TimeSpan.FromMinutes(30);

    public static TimeSpan ApplicationLifetime { get; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Who takes down their own recruitment waits this long before they put up another.
    /// </summary>
    public static TimeSpan RecruitPenalty { get; } = TimeSpan.FromMinutes(10);

    public static TimeSpan AwaiterLifetime { get; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// How long a new auto matching group waits for others before it asks its people whether they go.
    /// </summary>
    public static TimeSpan MatchingCollect { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the people of a group have to say whether they go.
    /// </summary>
    public static TimeSpan MatchingCheck { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long a group that everybody accepted waits, its leader named, before it becomes a party.
    /// </summary>
    public static TimeSpan MatchingStart { get; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The numbers of the texts the client shows, see its strings.
    /// </summary>
    public static class Text
    {
        public const int InvitedInMaze = 53001;
        public const int NotMaster = 53002;
        public const int Yourself = 53003;
        public const int InParty = 53004;
        public const int NotInParty = 53006;
        public const int ChangeMasterFailed = 53007;
        public const int Full = 53010;
        public const int NoMember = 53011;
        public const int Unexpected = 53012;
        public const int YouBlocked = 53013;
        public const int CannotParty = 53014;
        public const int AlreadyInviting = 53015;
        public const int CannotJoin = 53016;
        public const int InviterInMaze = 53017;
        public const int OtherInvitation = 53018;
        public const int NoApplicant = 53021;
        public const int RecruitWait = 53024;
        public const int NoRecruit = 53027;
        public const int Busy = 53028;
        public const int MemberInMaze = 53032;
    }

    /// <summary>
    /// The size an auto matching group of the maze must reach, from the people the maze admits: 2 for a maze of two, 4 for a
    /// maze of a full party, otherwise anything from two.
    /// </summary>
    public static int MatchingLimit(byte admission) => admission switch
    {
        2 => 2,
        3 => MaxMembers,
        _ => 0
    };
}
