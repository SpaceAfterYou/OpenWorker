using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace ow.Framework.Extensions
{
    public static class ConfigurationBuilderExtension
    {
        public static IConfigurationBuilder AddFramework(this IConfigurationBuilder builder, HostBuilderContext context)
        {
            builder.Sources.Clear();

            return builder
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("commonsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .SetBasePath(AppContext.BaseDirectory);
        }
    }
}