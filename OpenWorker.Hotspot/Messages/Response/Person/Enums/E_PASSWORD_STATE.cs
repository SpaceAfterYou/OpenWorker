namespace OpenWorker.Hotspot.Messages.Response.Person.Enums;

public enum E_PASSWORD_STATE : byte
{
    ePASSWORD_STATE_NONE = 0x0,
    ePASSWORD_STATE_UNAUTHENTICATED = 0x1,
    ePASSWORD_STATE_AUTHENTICATED = 0x2,
    ePASSWORD_STATE_LOCK = 0x3,
};