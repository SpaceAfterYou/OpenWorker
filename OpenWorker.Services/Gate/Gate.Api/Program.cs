using OpenWorker.Infrastructure.Database.Extensions;
using OpenWorker.Infrastructure.Communication.Hotspot.Extensions;
using OpenWorker.Infrastructure.Gameplay.Extensions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Extensions;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((builder, services) => services
        .AddPersistent(builder)
        .AddGameplay()
        .AddGateGameplay()
        .AddHotSpot());

var app = builder.Build();

await app.RunAsync();

