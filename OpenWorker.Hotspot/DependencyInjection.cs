using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Batch;

namespace OpenWorker.Hotspot;

public static class DependencyInjection
{
    public static IServiceCollection AddBatch(this IServiceCollection services)
    {
        return services
            .AddSingleton<MazeResourceProvider>()
            .AddSingleton<DistrictResourceProvider>()
            .AddSingleton<BatchManager>();
    }
}