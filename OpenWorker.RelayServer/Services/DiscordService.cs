using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;

namespace OpenWorker.RelayServer.Services;

public sealed class DiscordService(
    DiscordSocketClient client,
    IServiceProvider serviceProvider,
    IOptions<CredentialsOptions> credentials,
    ILogger<DiscordService> logger
) :
    BackgroundService
{
    private InteractionService InteractionService { get; } = new(client);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(client.Dispose);

        client.Log += e =>
        {
            logger.LogDebug(e.Exception, "{Message}", e.Message);
            return Task.CompletedTask;
        };

        client.InteractionCreated += async e =>
        {
            var ctx = new SocketInteractionContext(client, e);
            
            await InteractionService
                .ExecuteCommandAsync(ctx, serviceProvider)
                .ConfigureAwait(false);
        };

        client.Ready += async () =>
        {
            await InteractionService
                .AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider)
                .ConfigureAwait(false);

            await InteractionService
                .RegisterCommandsGloballyAsync()
                .ConfigureAwait(false);
        };

        await client
            .LoginAsync(TokenType.Bot, credentials.Value.DiscordToken)
            .ConfigureAwait(false);
        
        await client
            .StartAsync()
            .ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await client
            .LogoutAsync()
            .ConfigureAwait(false);
        
        await client
            .StopAsync()
            .ConfigureAwait(false);
    }
}