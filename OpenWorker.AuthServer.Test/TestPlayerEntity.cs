using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.DependencyInjection;
using OpenWorker.Gameplay.Modules.Login.Components;

namespace OpenWorker.AuthServer.Test;

[TestClass]
[DoNotParallelize]
public sealed class TestPlayerEntity
{
    [TestMethod]
    public void NewPlayerDoesNotInheritTheLoginOfAPreviousOne()
    {
        // The component type must be loaded before the provider scans the assemblies.
        _ = typeof(ClaimsComponent);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Instance:Target"] = "Auth" })
            .Build();

        var provider = new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance);

        using var world = World.Create();

        var previous = provider.CreateEntity(world);

        world.Set(previous, new ClaimsComponent(7, 1));
        world.Destroy(previous);

        var next = provider.CreateEntity(world);

        Assert.AreEqual(0, world.Get<ClaimsComponent>(next).Account);
    }
}
