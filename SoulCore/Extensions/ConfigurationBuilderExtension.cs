﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace SoulCore.Extensions
{
    public static class ConfigurationBuilderExtension
    {
        public static IConfigurationBuilder AddFrameworkConfig(this IConfigurationBuilder builder, HostBuilderContext context) => builder
            .AddJsonFile("config/commonsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"config/commonsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"config/appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .SetBasePath(AppContext.BaseDirectory);
    }
}
