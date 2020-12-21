﻿using Microsoft.Extensions.Configuration;

namespace ow.Framework.Extensions
{
    public static class ConfigurationBuilderExtension
    {
        public static IConfigurationBuilder AddFramework(this IConfigurationBuilder builder) =>
            builder.AddJsonFile("commonsettings.json", optional: false, reloadOnChange: true);
    }
}