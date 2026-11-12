using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;

namespace OpenWorker.RelayServer.Services;

/// <summary>
/// The Discord bot of the installation: its slash commands make accounts, gates and persons. The client calls the handlers
/// below on its own threads.
/// </summary>
public sealed partial class DiscordService(
    DiscordSocketClient client,
    IServiceProvider serviceProvider,
    IOptions<CredentialsOptions> credentials,
    ILogger<DiscordService> logger
) :
    BackgroundService
{
    private InteractionService InteractionService { get; } = new(client);

    /// <summary>
    /// The modules are added once: the client is ready again after every new session with Discord, and a module added twice
    /// is refused.
    /// </summary>
    private bool _modulesAdded;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(client.Dispose);

        client.Log += e =>
        {
            var level = Level(e.Severity);

            LogClient(logger, level, e.Exception, e.Source, e.Message);

            return Task.CompletedTask;
        };

        client.InteractionCreated += async e =>
        {
            var ctx = new SocketInteractionContext(client, e);

            await InteractionService.ExecuteCommandAsync(ctx, serviceProvider);
        };

        client.Ready += async () =>
        {
            if (!_modulesAdded)
            {
                await InteractionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

                _modulesAdded = true;
            }

            await InteractionService.RegisterCommandsGloballyAsync();
        };

        await client.LoginAsync(TokenType.Bot, credentials.Value.DiscordToken);

        await client.StartAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.LogoutAsync();

        await client.StopAsync();

        await base.StopAsync(cancellationToken);
    }

    private static LogLevel Level(LogSeverity severity) => severity switch
    {
        LogSeverity.Critical => LogLevel.Critical,
        LogSeverity.Error => LogLevel.Error,
        LogSeverity.Warning => LogLevel.Warning,
        LogSeverity.Info => LogLevel.Information,
        LogSeverity.Verbose => LogLevel.Debug,
        _ => LogLevel.Trace
    };

    [LoggerMessage(Message = "Discord {Source}: {Message}")]
    private static partial void LogClient(ILogger logger, LogLevel level, Exception? exception, string? source, string? message);
}
