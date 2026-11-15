using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Extensions.Data;
using Microsoft.Extensions.Configuration;
using OpenWorker.GateServer.Gameplay.Extensions;
using OpenWorker.GateServer.Server.Services;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Res.Extensions;

namespace OpenWorker.GateServer;

/// <summary>
/// The host of the gate, built and not yet started.
/// </summary>
public static class GateServerHost
{
    /// <summary>
    /// The game data the gate reads.
    /// </summary>
    public static IReadOnlyList<DataNeed> Data { get; } =
    [
        new DataNeed(DataFolders.ClientTables, "the server reads every client table at start", Refuse: true),
        new DataNeed(DataFolders.ServerTables, "a new character starts by the client tables only: the tutorial maze of its hero is not read"),
        new DataNeed(DataFolders.Maps, "Game:Prologue is on, but every new character skips the prologue and starts in its district",
            When: configuration => configuration.GetValue(GameOptions.SectionName + ":" + nameof(GameOptions.Prologue), true))
    ];

    public static IHostBuilder CreateBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .UseHealthEndpoint()
            .ConfigureServices(AddServices);
    }

    private static void AddServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddDataCheck(Data);
        services.AddCache();
        services.AddSessionCache();
        services.AddGateCache();
        services.AddChannelCache();
        services.AddDistrictCache();
        services.AddDistrictReserveCache();

        services.AddRes();
        services.AddBatch();

        services.AddPlayerTransfer();
        services.AddSingleton<ItemCatalog>();
        services.AddSingleton<DatabaseItemSerials>();
        services.AddSingleton<IItemSerials>(provider => provider.GetRequiredService<DatabaseItemSerials>());
        services.AddSingleton<InventoryFactory>();
        services.AddSingleton<InventoryPersistence>();
        services.AddPersistence();
        services.AddGameplay();

        services.AddGateOptions();
        services.AddGateEndpointOptions();
        services.AddPersonCreationOptions();
        services.AddRegistrationService<GateRegistrationService>();

        services.AddHotspot(context, EntityComponentService.Gate);
    }
}
