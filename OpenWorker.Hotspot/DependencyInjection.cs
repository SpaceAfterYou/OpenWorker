using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Hotspot;

public static class DependencyInjection
{
    public static IServiceCollection AddBatch(this IServiceCollection services)
    {
        return services
            .AddSingleton<MazeResourceProvider>()
            .AddSingleton<DistrictResourceProvider>()
            .AddSingleton<SkillActionProvider>()
            .AddSingleton<BatchManager>();
    }
}