using Core.Systems.GameSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GateService.Systems.ServiceSystem.Extensions
{
    internal static class ServiceCollectionExtension
    {
        internal static IServiceCollection AddTables(this IServiceCollection services, IConfiguration configuration)
        {
            BinTableProcessor BtProcessor = new(configuration);

            return services
                .AddSingleton(BtProcessor.ReadCustomizeHairTable())
                .AddSingleton(BtProcessor.ReadCustomizeEyesTable());
        }
    }
}