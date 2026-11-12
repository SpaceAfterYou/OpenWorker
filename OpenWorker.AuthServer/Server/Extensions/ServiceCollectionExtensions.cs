using OpenWorker.AuthServer.Server.Services;

namespace OpenWorker.AuthServer.Server.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static void AddGateSyncService(this IServiceCollection services)
    {
        services.AddHostedService<GateSyncService>();
    }
}