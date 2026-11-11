using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Stores;
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
/// to the account, kept hashed as the password of the account, and so do the wrong tries at it (<see cref="ITradePasswordTries"/>):
/// the tenth wrong one locks it for good, whatever the login, until a game master resets it, and every third wrong one
/// stops the tries for three minutes (<see cref="TradePasswordRules"/>). The player gives the password once a login: the
/// district and the maze share the answer through <see cref="ITradePasswordStates"/>, which every entry reads
/// (<see cref="EnterAsync"/>), so a password given in one holds in the other. The answer is the code (zero for success) and
/// the state, from which the client picks its message and goes on.
/// </summary>
public sealed class TradePasswordService(
    World world,
    IDbContextFactory<PersistenceContext> factory,
    ITradePasswordStates states,
    ITradePasswordTries tries,
    TimeProvider? time = null) :
    IHotspotHandler<PersonTradePasswordRequest>
{
    /// <summary>
    /// A request the state does not allow: a second password, or a check without one.
    /// </summary>
    public const ushort Refused = 1;

    /// <summary>
    /// The password is wrong (or is not tried just now): the state goes back to "not given". The code the account database
    /// answers with.
    /// </summary>
    public const ushort WrongPassword = 50105;

    /// <summary>
    /// Too many wrong passwords: the state is "locked". The code the account database answers with, for the answer that
    /// locks and for every answer after it.
    /// </summary>
    public const ushort Locked = 52003;

    private TimeProvider Time => time ?? TimeProvider.System;

    /// <summary>
    /// Takes up what the login has done with the password on another server: the player who gave it there has given it
    /// here. A lock is the account's and is read with the person at the entry. Called once the entry has set the person.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!world.TryGet(player, out TradePasswordComponent component) || !component.Has || component.State is PasswordProtectionState.Lock)
        {
            return;
        }

        var given = await states.IsGivenAsync(world.Get<ClaimsComponent>(player).Key, cancellationToken);

        if (!given || !world.IsAlive(player))
        {
            return;
        }

        world.Set(player, world.Get<TradePasswordComponent>(player) with { State = PasswordProtectionState.Authenticated });
    }

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
                await CheckAsync(context, component, request.Password);
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
            // Only an account without a password takes one: two servers cannot both set it. A new password starts without wrong tries.
            var set = await database.Accounts
                .Where(e => e.Id == account && e.TradePassword == string.Empty)
                .ExecuteUpdateAsync(
                    e => e.SetProperty(x => x.TradePassword, encoded)
                        .SetProperty(x => x.TradePasswordFailed, (short)0)
                        .SetProperty(x => x.TradePasswordFailedAt, (DateTime?)null),
                    context.CancellationToken);

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

        // The password just made is the password given: the other servers of this login find it so.
        await states.GiveAsync(world.Get<ClaimsComponent>(player).Key, context.CancellationToken);

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

    private async ValueTask CheckAsync(ServiceHandleContext context, TradePasswordComponent component, string password)
    {
        var player = context.Player;

        if (component.State is PasswordProtectionState.None)
        {
            Send(player, component.State, Refused);
            return;
        }

        // A password that is not four of the digits of the pad is no attempt: it is refused with its own code and counts for nothing.
        if (LoginRules.ValidateTradePassword(password) is var error and not 0)
        {
            Send(player, component.State, error);
            return;
        }

        // The lock is not lifted by the right password: the answer to every attempt is the lock.
        if (component.State is PasswordProtectionState.Lock)
        {
            Send(player, component.State, Locked);
            return;
        }

        var account = world.Get<ClaimsComponent>(player).Account;
        var session = world.Get<ClaimsComponent>(player).Key;
        var now = Time.GetUtcNow().UtcDateTime;

        // What other logins have done meanwhile counts: the account keeps the tries.
        var before = await tries.LoadAsync(account, context.CancellationToken);

        if (!world.IsAlive(player))
        {
            return;
        }

        if (TradePasswordRules.IsLocked(before.Failed))
        {
            SetState(player, PasswordProtectionState.Lock, before.Failed);
            Send(player, PasswordProtectionState.Lock, Locked);
            return;
        }

        // After every third wrong password the tries stop for a while, and the ones made then count for nothing.
        if (TradePasswordRules.IsPaused(before.Failed, before.LastFailedAt, now))
        {
            Send(player, component.State, WrongPassword);
            return;
        }

        var stored = component.Password;
        var right = await Task.Run(() => PasswordHash.VerifyEncoded(password, stored));

        if (!world.IsAlive(player))
        {
            return;
        }

        if (right)
        {
            await tries.ClearAsync(account, context.CancellationToken);
            await states.GiveAsync(session, context.CancellationToken);

            if (world.IsAlive(player))
            {
                SetState(player, PasswordProtectionState.Authenticated, 0);
                Send(player, PasswordProtectionState.Authenticated, 0);
            }

            return;
        }

        var failed = await tries.FailAsync(account, now, context.CancellationToken);

        await states.TakeBackAsync(session, context.CancellationToken);

        if (!world.IsAlive(player))
        {
            return;
        }

        var locked = TradePasswordRules.IsLocked(failed);
        var state = locked ? PasswordProtectionState.Lock : PasswordProtectionState.UnAuthenticated;

        SetState(player, state, failed);
        Send(player, state, locked ? Locked : WrongPassword);
    }

    private void SetState(Entity player, PasswordProtectionState state, int failed)
    {
        world.Set(player, world.Get<TradePasswordComponent>(player) with { State = state, FailedAttempts = failed });
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
