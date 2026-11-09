namespace OpenWorker.Hotspot.Modules.ItemSetup.Enums;

/// <summary>
/// What an upgrade did to the item, as the client shows it.
/// </summary>
public enum ItemSetupUpgradeResult : byte
{
    Success = 1,
    Failure = 2,
    Broken = 3,

    /// <summary>
    /// The item that protected the upgraded one broke instead of it.
    /// </summary>
    ProtectionBroken = 4
}
