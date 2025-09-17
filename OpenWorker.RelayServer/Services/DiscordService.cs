using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenWorker.RelayServer.Services;

public sealed class DiscordService(
    DiscordSocketClient client,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
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
            .LoginAsync(TokenType.Bot, configuration["Credentials:DiscordToken"])
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