using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.RelayServer.DependencyInjection;

public static class Discord
{
    public static IServiceCollection AddDiscord(this IServiceCollection services)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged
        };

        return services
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>();
    }
}