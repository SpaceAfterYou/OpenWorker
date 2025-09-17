namespace OpenWorker.Hotspot.Enums;

public enum LoginErrorMessageCode : int
{
    None = 0,
    WrongUsernameOrPassword = 1,
    InGameAlready = 2,
    BanAccount = 3,
    BanIp = 4,
    BanMac = 5,
    WrongMac = 6,
    CheckSystem = 7
}