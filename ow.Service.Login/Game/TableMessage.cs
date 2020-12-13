namespace ow.Service.Login.Game
{
    /// <summary>
    /// Field Ids from datas/data12.v/../bin/Tables/tb_UI_String.res
    /// </summary>
    public enum TableMessage : uint
    {
        None,
        LoginFailed = 50104,
        WrongUsernameOrPassword = 50105,
        TheIdYouEnteredIsAlreadyConnectedToTheServer = 50106,
        AccountIsBlocked = 50109,
        IpIsBlocked = 50110,

        /// Forgot ID
        MacIsBlocked = 50110
    }
}
