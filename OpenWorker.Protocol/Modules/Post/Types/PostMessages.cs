namespace OpenWorker.Hotspot.Modules.Post.Types;

/// <summary>
/// The numbers of the texts (the UI strings of the client) shown when a request about letters does not go through. They
/// travel in place of a result (sending, sending back) or as the code of an error.
/// </summary>
public static class PostMessages
{
    /// <summary>
    /// The mail has attachments that were not taken.
    /// </summary>
    public const int HasAttachments = 2262;

    public const int ToYourself = 2266;

    /// <summary>
    /// A letter that came back once does not go back again.
    /// </summary>
    public const int AlreadyReturned = 2276;

    /// <summary>
    /// A letter of the game does not go back.
    /// </summary>
    public const int FromTheGame = 2277;

    public const int NoReceiver = 55301;

    /// <summary>
    /// Not enough gold for the money of the letter and the postage.
    /// </summary>
    public const int NotEnoughGold = 55302;

    public const int CannotBeTraded = 55303;

    public const int Bound = 55304;

    /// <summary>
    /// The letter is gone. Also the answer to a letter the server does not take at all, for which the client has no text.
    /// </summary>
    public const int Deleted = 55306;

    public const int TooMuchGold = 55307;

    public const int NoRoom = 55308;

    public const int AccountLetterNotDeleted = 55619;
}
