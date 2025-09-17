using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Persistence;

namespace OpenWorker.RelayServer.Interactions.Discord;

[Group("soulworker-person", "SoulWorker person management")]
public sealed class PersonInteractions(IDbContextFactory<PersistenceContext> factory) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("rename", "Rename existing person")]
    public async Task ChangePasswordAsync(
        [Summary("current-name", "Current name of the person")] string current,
        [Summary("new-name", "New name of the person")] string name)
    {
        await using var context = await factory
            .CreateDbContextAsync()
            .ConfigureAwait(false);

        var account = await context.Accounts
            .Include(x => x.Persons)
            .FirstOrDefaultAsync(x => x.Discord == Context.User.Id)
            .ConfigureAwait(false);

        if (account is null)
        {
            await RespondAsync("Account not found", ephemeral: true).ConfigureAwait(false);

            return;
        }

        var person = account.Persons.FirstOrDefault(x => x.Name == current);
        
        if (person is null)
        {
            await RespondAsync("Person not found", ephemeral: true).ConfigureAwait(false);

            return;
        }
        
        person.Name = name;

        await context
            .SaveChangesAsync()
            .ConfigureAwait(false);

        await RespondAsync("Person has been renamed.", ephemeral: true).ConfigureAwait(false);
    }
}