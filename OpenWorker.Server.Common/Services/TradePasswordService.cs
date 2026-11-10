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
/// to the account, kept hashed as the password of the account. The player gives it once a login: the district and the maze
/// share the answer through <see cref="ITradePasswordStates"/>, which every entry reads (<see cref="EnterAsync"/>), so a
/// password given in one holds in the other, and five wrong ones lock it for the rest of the login wherever the player goes
/// (<c>docs/notes-district-gaps.md</c>). The answer is the code (zero for success) and the state, from which the client picks
/// its message and goes on.
/// </summary>
public sealed class TradePasswordService(World world, IDbContextFactory<PersistenceContext> factory, ITradePasswordStates states) :
    IHotspotHandler<PersonTradePasswordRequest>
{
    public const int MaxFailedAttempts = 5;

    /// <summary>
    /// A request the state does not allow: a second password, or a check without one.
    /// </summary>
    public const ushort Refused = 1;

    /// <summary>
    /// The password is wrong: the state goes back to "not given". The code the account database answers with.
    /// </summary>
    public const ushort WrongPassword = 50105;

    /// <summary>
    /// Too many wrong passwords: the state is "locked". The code the account database answers with, for the answer that
    /// locks and for every answer after it.
    /// </summary>
    public const ushort Locked = 52003;

    /// <summary>
    /// Takes up what the login has done with the password on another server: the player who gave it there has given it
    /// here, and the one who was locked is locked. Called once the entry has set the person.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!world.TryGet(player, out TradePasswordComponent component) || !component.Has)
        {
            return;
        }

        var record = await states.LoadAsync(world.Get<ClaimsComponent>(player).Key, cancellationToken);

        if (record is not { } known || !world.IsAlive(player))
        {
            return;
        }

        world.Set(player, world.Get<TradePasswordComponent>(player) with { State = StateOf(known), FailedAttempts = known.Failed });
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

        // The password just made is the password given: the other servers of this login find it so.
        await states.AuthenticateAsync(world.Get<ClaimsComponent>(player).Key, context.CancellationToken);

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

        var stored = component.Password;
        var right = await Task.Run(() => PasswordHash.VerifyEncoded(password, stored));
        var session = world.Get<ClaimsComponent>(player).Key;

        if (!world.IsAlive(player))
        {
            return;
        }

        var record = right
            ? await states.AuthenticateAsync(session)
            : await states.FailAsync(session, MaxFailedAttempts);

        if (!world.IsAlive(player))
        {
            return;
        }

        var state = StateOf(record);

        world.Set(player, world.Get<TradePasswordComponent>(player) with { State = state, FailedAttempts = record.Failed });

        Send(player, state, state switch
        {
            PasswordProtectionState.Authenticated => (ushort)0,
            PasswordProtectionState.Lock => Locked,
            _ => WrongPassword
        });
    }

    /// <summary>
    /// Where the player stands for a record of its login: locked, given, or not given (a record that is neither is a
    /// player who has missed).
    /// </summary>
    private static PasswordProtectionState StateOf(TradePasswordRecord record)
    {
        return record.Locked
            ? PasswordProtectionState.Lock
            : record.Authenticated ? PasswordProtectionState.Authenticated : PasswordProtectionState.UnAuthenticated;
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
