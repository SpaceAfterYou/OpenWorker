using Microsoft.Extensions.Hosting;
using OpenWorker.RelayServer;

await RelayServerHost.CreateBuilder(args).Build().RunAsync();
