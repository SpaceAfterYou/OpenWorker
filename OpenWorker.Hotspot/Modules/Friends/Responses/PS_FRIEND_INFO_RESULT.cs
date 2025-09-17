using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Friends.Enums;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

public struct PS_FRIEND_INFO_RESULT
{
    public FriendResult nResult;
    public ST_OTHER_CHARINFO stOtherCharInfo;
    public TitleValue stTitle;
}