using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.IO.Network.Sync.Providers;
using System;

namespace ow.Framework.IO.Network.Sync.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddAccountContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddContext<AccountContext>(context);

        public static IServiceCollection AddCharacterContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddContext<CharacterContext>(context);

        public static IServiceCollection AddItemContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddContext<ItemContext>(context);

        public static IServiceCollection AddFramework(this IServiceCollection services) => services
            .AddSingleton<HandlerProvider>();

        private static IServiceCollection AddContext<TContext>(this IServiceCollection services, HostBuilderContext context) where TContext : notnull, DbContext => services
                .AddDbContextPool<TContext>(options => options
                    .UseNpgsql(context.Configuration.GetConnectionString("PgsqlConnection"), b => b.EnableRetryOnFailure()))
                .AddTransient<Func<TContext>>(s => s.GetRequiredService<TContext>);
    }
}