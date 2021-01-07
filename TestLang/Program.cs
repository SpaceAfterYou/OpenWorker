using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.IO.File.Bin;
using System.Threading.Tasks;

namespace TestLang
{
    public class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddTransient<BinTable>());
    }
}