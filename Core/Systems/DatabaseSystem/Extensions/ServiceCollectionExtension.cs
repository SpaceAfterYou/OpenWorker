using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Systems.DatabaseSystem.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services) => services
            .AddDbContext<Context>(options => options.UseNpgsql(""));
    }
}