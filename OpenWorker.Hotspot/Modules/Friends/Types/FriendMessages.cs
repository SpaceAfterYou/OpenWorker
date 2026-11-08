namespace OpenWorker.Hotspot.Modules.Friends.Types;

/// <summary>
/// The numbers of the texts the client shows when a request about friends does not go through. They travel in place of a result,
/// or as the code of an error.
/// </summary>
public static class FriendMessages
{
    public const int AlreadyFriend = 55101;
    public const int InMaze = 55102;
    public const int YouHaveTooMany = 55103;
    public const int NotOnServer = 55104;
    public const int AlreadyBlocked = 55105;
    public const int TooManyBlocked = 55106;
    public const int Refused = 55107;
    public const int NoSuchPerson = 55109;
    public const int AlreadyAsked = 55111;
    public const int TheyHaveTooMany = 55114;
    public const int FriendsCannotBeBlocked = 55115;
    public const int DoesNotTakeFriends = 59202;
}
