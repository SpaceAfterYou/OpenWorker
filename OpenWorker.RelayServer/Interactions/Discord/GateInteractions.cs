using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence;

namespace OpenWorker.RelayServer.Interactions.Discord;

[DefaultMemberPermissions(GuildPermission.Administrator)]
[Group("soulworker-gate", "SoulWorker gate management")]
public sealed class GateInteractions(IDbContextFactory<PersistenceContext> factory) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("create", "Create a new gate")]
    public async Task CreateAsync([Summary("name", "The name of the gate")] string name)
    {
        await using var context = await factory
            .CreateDbContextAsync();

        if (await context.Gates.AnyAsync(x => x.Name == name))
        {
            await RespondAsync("Gate with this name already exists.", ephemeral: true);
        }

        else
        {
            var gate = new GatePersistent
            {
                Name = name
            };
                
            await context.Gates
                .AddAsync(gate);

            await context
                .SaveChangesAsync();

            await RespondAsync($"Created gate with name {name}.", ephemeral: true);
        }
    }

    [SlashCommand("delete", "Delete a gate")]
    public async Task DeleteAsync([Summary("id", "The id of the gate")] int id)
    {
        await using var context = await factory
            .CreateDbContextAsync();

        var gate = await context.Gates
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        
        if (gate is null)
        {
            await RespondAsync("Gate not found.", ephemeral: true);
        }

        else
        {
            context.Gates.Remove(gate);

            await context
                .SaveChangesAsync();

            await RespondAsync("Gate has been deleted.", ephemeral: true);
        }
    }
}