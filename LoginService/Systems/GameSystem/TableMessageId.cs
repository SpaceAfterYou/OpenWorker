namespace LoginService.Systems.GameSystem
{
    // Ids from tb_UI_String
    public enum TableMessageId : uint
    {
        None,
        LoginFailed = 50104,
        WrongUsernameOrPassword = 50105,
        TheIdYouEnteredIsAlreadyConnectedToTheServer = 50106,
        AccountIsBlocked = 50109,
        IpIsBlocked = 50110,
        MacIsBlocked = 50110 // Forgot ID
    }
}