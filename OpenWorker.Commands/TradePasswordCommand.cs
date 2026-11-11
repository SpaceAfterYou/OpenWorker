using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;

namespace OpenWorker.Commands;

/// <summary>
/// Takes the trade password away from the account of the player and forgets the wrong tries: <c>/tradepassword reset</c>.
/// A locked password stays locked until this is done, as it stays until the password is reset on the page of the account
/// in the original.
/// </summary>
[CommandTrigger("tradepassword")]
[CommandDescription("Reset the trade password of your account: no password, no wrong tries")]
internal sealed partial class TradePasswordCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private ITradePasswordTries Tries { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!string.Equals(tokens.ElementAtOrDefault(0), "reset", StringComparison.Ordinal))
        {
            return false;
        }

        await Tries.ResetAsync(World.Get<ClaimsComponent>(player).Account);

        if (!World.IsAlive(player))
        {
            return true;
        }

        World.Set(player, World.Get<TradePasswordComponent>(player) with { Password = string.Empty, State = PasswordProtectionState.None, FailedAttempts = 0 });
        World.Get<ServerSessionComponent>(player).Send(new CharacterTradePasswordResponse { ProtectionState = PasswordProtectionState.None, ErrorCode = 0 });

        return true;
    }
}
