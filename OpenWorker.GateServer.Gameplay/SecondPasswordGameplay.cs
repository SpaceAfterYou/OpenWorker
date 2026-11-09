using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Login;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.GateServer.Gameplay;

public sealed class SecondPasswordGameplay(
    World world,
    IDbContextFactory<PersistenceContext> factory,
    ServerContent contents)
{
    private const int MaxFailedAttempts = 5;

    private bool IsEnabled => contents[ServerContentIndex.SecondPassword];

    public void Load(Entity player, AccountPersistent account)
    {
        var hasPassword = string.IsNullOrEmpty(account.SecondPassword) is false;

        world.Set(player, new SecondPasswordComponent
        {
            Password = account.SecondPassword,
            State = IsEnabled switch
            {
                false => PasswordProtectionState.Authenticated,
                true when hasPassword => PasswordProtectionState.UnAuthenticated,
                true => PasswordProtectionState.None
            }
        });
    }

    public bool IsSatisfied(Entity player)
    {
        return !IsEnabled || world.Get<SecondPasswordComponent>(player).State is PasswordProtectionState.Authenticated;
    }

    public async Task HandleAsync(ServiceHandleContext context, PersonSecondPasswordRequest request)
    {
        if (!IsEnabled)
        {
            Send(context.Player, PasswordProtectionState.Authenticated, 0);
            return;
        }

        switch (request.CheckType)
        {
            case PasswordCheckType.Create:
                await CreateAsync(context, request.Password);
                break;

            case PasswordCheckType.Check:
                await CheckAsync(context.Player, request.Password);
                break;

            case PasswordCheckType.Continue:
            case PasswordCheckType.StateCheck:
            default:
                Send(context.Player, world.Get<SecondPasswordComponent>(context.Player).State, 0);
                break;
        }
    }

    private async Task CreateAsync(ServiceHandleContext context, string password)
    {
        var component = world.Get<SecondPasswordComponent>(context.Player);

        if (component.Has)
        {
            Send(context.Player, component.State, (int)PersonErrorCode.Failed);
            return;
        }

        if (LoginRules.ValidateSecondPassword(password) is { } error)
        {
            Send(context.Player, component.State, (int)error);
            return;
        }

        // Hashing takes long enough to delay everybody else on the world loop.
        var encoded = await Task.Run(() => PasswordHash.CreateEncoded(password));
        var accountId = world.Get<ClaimsComponent>(context.Player).Account;

        await using var database = await factory
            .CreateDbContextAsync(context.CancellationToken);

        var account = await database.Accounts
            .AsTracking()
            .FirstAsync(e => e.Id == accountId, context.CancellationToken);

        account.SecondPassword = encoded;

        await database
            .SaveChangesAsync(context.CancellationToken);

        world.Set(context.Player, component with
        {
            Password = encoded,
            State = PasswordProtectionState.Authenticated
        });

        Send(context.Player, PasswordProtectionState.Authenticated, 0);
    }

    private async Task CheckAsync(Entity player, string password)
    {
        var component = world.Get<SecondPasswordComponent>(player);

        if (component.State is PasswordProtectionState.Lock || !component.Has)
        {
            Send(player, component.State, (int)PersonErrorCode.Failed);
            return;
        }

        var stored = component.Password;

        if (await Task.Run(() => PasswordHash.VerifyEncoded(password, stored)))
        {
            world.Set(player, component with
            {
                State = PasswordProtectionState.Authenticated,
                FailedAttempts = 0
            });

            Send(player, PasswordProtectionState.Authenticated, 0);
            return;
        }

        var attempts = component.FailedAttempts + 1;

        var state = attempts >= MaxFailedAttempts
            ? PasswordProtectionState.Lock
            : PasswordProtectionState.UnAuthenticated;

        world.Set(player, component with { State = state, FailedAttempts = attempts });

        // The client chooses its message by the state, so any non-zero code will do.
        Send(player, state, (int)PersonErrorCode.SecondPasswordInvalid);
    }

    private void Send(Entity player, PasswordProtectionState state, int error)
    {
        world.Get<ServerSessionComponent>(player).Send(new CharacterSecondPasswordResponse
        {
            ProtectionState = state,
            ErrorCode = error
        });
    }
}
