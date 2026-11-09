using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Gameplay.Modules.Login;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The password the client asks before a trade, a private shop or breaking an item, in a district and in a maze. It belongs
/// to the account, kept hashed as the password of the account; on each server the player gives it once, and five wrong
/// ones lock it until the next entry. The answer is the code (zero for success) and the state, from which the client picks
/// its message and goes on.
/// </summary>
public sealed class TradePasswordService(World world, IDbContextFactory<PersistenceContext> factory) :
    IHotspotHandler<PersonTradePasswordRequest>
{
    public const int MaxFailedAttempts = 5;

    /// <summary>
    /// A request the state does not allow: a second password, or a check without one.
    /// </summary>
    public const ushort Refused = 1;

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonTradePasswordRequest request)
    {
        var player = context.Player;
        var component = world.Get<TradePasswordComponent>(player);

        switch (request.CheckType)
        {
            case PasswordCheckType.Create:
                await CreateAsync(context, component, request.Password);
                break;

            case PasswordCheckType.Check:
                await CheckAsync(player, component, request.Password);
                break;

            default:
                Send(player, component.State, 0);
                break;
        }
    }

    private async ValueTask CreateAsync(ServiceHandleContext context, TradePasswordComponent component, string password)
    {
        var player = context.Player;

        if (component.State is not PasswordProtectionState.None)
        {
            Send(player, component.State, Refused);
            return;
        }

        if (LoginRules.ValidateTradePassword(password) is var error and not 0)
        {
            Send(player, component.State, error);
            return;
        }

        // Hashing takes long enough to delay everybody else on the world loop.
        var encoded = await Task.Run(() => PasswordHash.CreateEncoded(password));
        var account = world.Get<ClaimsComponent>(player).Account;

        await using (var database = await factory.CreateDbContextAsync(context.CancellationToken))
        {
            // Only an account without a password takes one: two servers cannot both set it.
            var set = await database.Accounts
                .Where(e => e.Id == account && e.TradePassword == string.Empty)
                .ExecuteUpdateAsync(e => e.SetProperty(x => x.TradePassword, encoded), context.CancellationToken);

            if (set == 0)
            {
                if (world.IsAlive(player))
                {
                    Send(player, world.Get<TradePasswordComponent>(player).State, Refused);
                }

                return;
            }
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        world.Set(player, world.Get<TradePasswordComponent>(player) with
        {
            Password = encoded,
            State = PasswordProtectionState.Authenticated,
            FailedAttempts = 0
        });

        Send(player, PasswordProtectionState.Authenticated, 0);
    }

    private async ValueTask CheckAsync(Entity player, TradePasswordComponent component, string password)
    {
        if (component.State is PasswordProtectionState.None or PasswordProtectionState.Lock)
        {
            Send(player, component.State, Refused);
            return;
        }

        var stored = component.Password;
        var right = LoginRules.ValidateTradePassword(password) == 0 && await Task.Run(() => PasswordHash.VerifyEncoded(password, stored));

        if (!world.IsAlive(player))
        {
            return;
        }

        component = world.Get<TradePasswordComponent>(player);

        if (right)
        {
            world.Set(player, component with { State = PasswordProtectionState.Authenticated, FailedAttempts = 0 });
            Send(player, PasswordProtectionState.Authenticated, 0);

            return;
        }

        var attempts = component.FailedAttempts + 1;
        var state = attempts >= MaxFailedAttempts ? PasswordProtectionState.Lock : component.State;

        world.Set(player, component with { State = state, FailedAttempts = attempts });

        Send(player, state, LoginRules.TradePasswordInvalid);
    }

    private void Send(Entity player, PasswordProtectionState state, ushort error)
    {
        world.Get<ServerSessionComponent>(player).Send(new CharacterTradePasswordResponse
        {
            ProtectionState = state,
            ErrorCode = error
        });
    }
}
