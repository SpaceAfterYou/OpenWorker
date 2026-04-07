namespace OpenWorker.Hotspot.Messages.Response.Person.Enums;

public enum PasswordProtectionState : byte
{
    None = 0x0,
    UnAuthenticated = 0x1,
    Authenticated = 0x2,
    Lock = 0x3,
};
