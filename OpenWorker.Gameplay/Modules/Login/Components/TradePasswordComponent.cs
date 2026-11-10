using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;

namespace OpenWorker.Gameplay.Modules.Login.Components;

[EntityComponent(EntityComponentService.Gate)]
[EntityComponent(EntityComponentService.District)]
public readonly record struct TradePasswordComponent
{
    /// <summary>
    /// The hash of the password, empty while the account has none.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Where the player stands with the password on this server: none set, not given yet, given, or locked after too many
    /// wrong ones.
    /// </summary>
    public PasswordProtectionState State { get; init; }

    public int FailedAttempts { get; init; }

    public bool Has => string.IsNullOrEmpty(Password) is false;

    /// <summary>
    /// The player has given the password of its account on this login: the only state in which a trade or a private shop is
    /// allowed. A player with no password is not allowed either; it makes one first.
    /// </summary>
    public bool Allows => State == PasswordProtectionState.Authenticated;
}