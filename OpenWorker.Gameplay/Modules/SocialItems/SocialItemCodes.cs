namespace OpenWorker.Gameplay.Modules.SocialItems;

/// <summary>
/// The codes the client shows when a request of the social items is refused, as the original server gives them. Every refusal
/// of the group is the error answer of 2D/01.
/// </summary>
public static class SocialItemCodes
{
    /// <summary>The channel holds as many objects as it may.</summary>
    public const ushort TooMany = 59300;

    /// <summary>The object cannot stand there: a place kept free, no ground, too far from the player or on another object.</summary>
    public const ushort BadPlace = 59301;

    /// <summary>Objects are put down and used in a district only.</summary>
    public const ushort NotDistrict = 59302;

    /// <summary>The object has no room for the player, or the player uses one already.</summary>
    public const ushort Full = 59304;

    /// <summary>A stop outside a district.</summary>
    public const ushort StopNotDistrict = 59305;

    /// <summary>The item, the object or the place of the player is not what it should be.</summary>
    public const ushort Failed = 59306;

    /// <summary>The social item is unknown, or the player uses no object.</summary>
    public const ushort Unknown = 59308;
}
