using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
            .CreateDbContextAsync();

        if (await context.Accounts.AnyAsync(x => x.Discord == Context.User.Id))
        {
            var message = $"The account with your Discord ID already exists \"{Context.User.Id}\", but it doesn't seem to belong to you...";
            await RespondAsync(message, ephemeral: true);
        }

        else if (await context.Accounts.AnyAsync(x => x.Username == Context.User.Username))
        {
            var message = $"The account with your username already exists \"{Context.User.Username}\", but it doesn't seem to belong to you...";

            await RespondAsync(message, ephemeral: true);
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
            });

            try
            {
                await context.SaveChangesAsync();
            }

            // Somebody took the name between the check above and now.
            catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await RespondAsync($"The account with your username already exists \"{Context.User.Username}\".", ephemeral: true);
                return;
            }

            await RespondAsync($"Login: {Context.User.Username}.", ephemeral: true);
        }
    }

    [SlashCommand("change-password", "Change your account password")]
    public async Task ChangePasswordAsync([Summary("password", "Your account password")] string password)
    {
        await using var context = await factory
            .CreateDbContextAsync();

        var account = await context.Accounts
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Discord == Context.User.Id);

        if (account is null)
        {
            await RespondAsync("Account not found", ephemeral: true);
        }

        else
        {
            PasswordHash.Create(password, out var passwordHash, out var saltHash);

            account.PasswordHash = passwordHash;
            account.SaltHash = saltHash;

            await context
                .SaveChangesAsync();

            await RespondAsync("Password changed.", ephemeral: true);
        }
    }
}