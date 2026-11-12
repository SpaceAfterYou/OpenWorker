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
            .CreateDbContextAsync();

        var account = await context.Accounts
            .AsTracking()
            .Include(x => x.Persons)
            .FirstOrDefaultAsync(x => x.Discord == Context.User.Id);

        if (account is null)
        {
            await RespondAsync("Account not found", ephemeral: true);

            return;
        }

        var person = account.Persons.FirstOrDefault(x => x.Name == current);
        
        if (person is null)
        {
            await RespondAsync("Person not found", ephemeral: true);

            return;
        }
        
        person.Name = name;

        await context
            .SaveChangesAsync();

        await RespondAsync("Person has been renamed.", ephemeral: true);
    }
}