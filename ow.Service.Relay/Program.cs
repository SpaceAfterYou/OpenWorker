using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Framework.IO.Network.Sync.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ow.RelayService
{
    public sealed class RelayServer : Server
    {
        public RelayServer()
        {
            IEnumerable<Type> methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsDefined(typeof(HandlerAttribute)));
        }
    }

    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => config.AddFrameworkConfig(context))
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddSyncHandlers()
                .AddRelayHandlers());
    }
}