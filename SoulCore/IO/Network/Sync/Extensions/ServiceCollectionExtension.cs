﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoulCore.Database.AccouintPosts;
using SoulCore.Database.Accounts;
using SoulCore.Database.CharacterPosts;
using SoulCore.Database.Characters;
using SoulCore.Database.Guilds;
using SoulCore.Database.Storages;
using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SoulCore.IO.Network.Sync.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddGuildContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddPooledDbContext<GuildContext>(context);

        public static IServiceCollection AddAccountPostContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddPooledDbContext<AccountPostContext>(context);

        public static IServiceCollection AddCharacterPostContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddPooledDbContext<CharacterPostContext>(context);

        public static IServiceCollection AddAccountContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddPooledDbContext<AccountContext>(context);

        public static IServiceCollection AddCharacterContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddPooledDbContext<CharacterContext>(context);

        public static IServiceCollection AddItemContext(this IServiceCollection services, HostBuilderContext context) => services
            .AddPooledDbContext<ItemContext>(context);

        public static IServiceCollection AddSyncHandlers(this IServiceCollection services)
        {
            foreach (Type type in GetSyncHandlers())
                services.AddTransient(type);

            return services.AddSingleton<HandlerProvider>();
        }

        private static IEnumerable<Type> GetSyncHandlers() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(type => type.IsDefined(typeof(HandlerAttribute)))
            .Select(t => t.DeclaringType!)
            .Distinct();

        public static IServiceCollection AddPooledDbContext<TContext>(this IServiceCollection services, HostBuilderContext context) where TContext : notnull, DbContext => services
            .AddPooledDbContextFactory<TContext>(options => options
                .UseNpgsql(context.Configuration.GetConnectionString("PgsqlConnection"), b => b.EnableRetryOnFailure()));
    }
}
