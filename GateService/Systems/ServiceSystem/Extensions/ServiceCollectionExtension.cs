using Microsoft.Extensions.DependencyInjection;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace GateService.Systems.ServiceSystem.Extensions
{
    internal static class ServiceCollectionExtension
    {
        internal static IServiceCollection AddTables(this IServiceCollection services)
        {
            VArchive archive = new("", "");

            return services;
        }
    }
}