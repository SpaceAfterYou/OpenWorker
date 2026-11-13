using Microsoft.Extensions.Hosting;
using OpenWorker.GateServer;

await GateServerHost.CreateBuilder(args).Build().RunAsync();
