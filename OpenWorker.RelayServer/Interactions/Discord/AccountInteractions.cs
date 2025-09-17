using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.RelayServer.Interactions.Discord;

[Group("soulworker-account", "SoulWorker account management")]
public sealed class AccountInteractions(IDbContextFactory<PersistenceContext> factory) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("create", "Create a new account")]
    public async Task CreateAsync([Summary("password", "Your account password")] string password)
    {
        await using var context = await factory
            .CreateDbContextAsync()
            .ConfigureAwait(false);

        if (await context.Accounts.AnyAsync(x => x.Discord == Context.User.Id).ConfigureAwait(false))
        {
            var message = $"The account with your Discord ID already exists \"{Context.User.Id}\", but it doesn't seem to belong to you...";
            await RespondAsync(message, ephemeral: true).ConfigureAwait(false);
        }

        else if (await context.Accounts.AnyAsync(x => x.Username == Context.User.Username).ConfigureAwait(false))
        {
            var message = $"The account with your username already exists \"{Context.User.Username}\", but it doesn't seem to belong to you...";

            await RespondAsync(message, ephemeral: true).ConfigureAwait(false);
        }

        else
        {
            PasswordHash.Create(password, out var passwordHash, out var saltHash);

            await context.Accounts.AddAsync(new AccountPersistent
            {
                Username = Context.User.Username,
                Discord = Context.User.Id,
                PasswordHash = passwordHash,
                SaltHash = saltHash
            }).ConfigureAwait(false);

            await context.SaveChangesAsync().ConfigureAwait(false);

            await RespondAsync($"Login: {Context.User.Username}.", ephemeral: true).ConfigureAwait(false);
        }
    }

    [SlashCommand("change-password", "Change your account password")]
    public async Task ChangePasswordAsync([Summary("password", "Your account password")] string password)
    {
        await using var context = await factory
            .CreateDbContextAsync()
            .ConfigureAwait(false);

        var account = await context.Accounts
            .FirstOrDefaultAsync(x => x.Discord == Context.User.Id)
            .ConfigureAwait(false);

        if (account is null)
        {
            await RespondAsync("Account not found", ephemeral: true).ConfigureAwait(false);
        }

        else
        {
            PasswordHash.Create(password, out var passwordHash, out var saltHash);

            account.PasswordHash = passwordHash;
            account.SaltHash = saltHash;

            await context
                .SaveChangesAsync()
                .ConfigureAwait(false);

            await RespondAsync("Password changed.", ephemeral: true).ConfigureAwait(false);
        }
    }
}