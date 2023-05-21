using OpenWorker.Infrastructure.Database.Extensions;
using OpenWorker.Infrastructure.Communication.Hotspot.Extensions;
using OpenWorker.Infrastructure.Gameplay.Extensions;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Extensions;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((builder, services) => services
        .AddPersistent(builder)
        .AddHotSpot()
        .AddGameplay()
        .AddAuthGameplay());

var app = builder.Build();

await app.RunAsync();

// https://youtu.be/egpdUR24ETM
