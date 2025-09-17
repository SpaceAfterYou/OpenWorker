using OpenWorker.AuthServer.Server.Services;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.AuthServer.Server.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static void AddGateSyncService(this IServiceCollection services)
    {
        services.AddSingleton<List<GateInfo>>();
        services.AddHostedService<GateSyncService>();
    }
}