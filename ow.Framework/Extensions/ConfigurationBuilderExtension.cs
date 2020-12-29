using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace ow.Framework.Extensions
{
    public static class ConfigurationBuilderExtension
    {
        public static IConfigurationBuilder AddFramework(this IConfigurationBuilder builder, HostBuilderContext context) => builder
            .AddJsonFile("config/commonsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"config/appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .SetBasePath(AppContext.BaseDirectory);
    }
}