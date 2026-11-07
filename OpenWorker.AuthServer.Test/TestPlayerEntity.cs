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

    [TestMethod]
    public void ComponentSetOnOnePlayerStaysWithThatPlayer()
    {
        _ = typeof(ClaimsComponent);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Instance:Target"] = "Auth" })
            .Build();

        var provider = new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance);

        using var world = World.Create();

        var players = Enumerable.Range(0, 5).Select(_ => provider.CreateEntity(world)).ToArray();

        for (var index = 0; index < players.Length; index++)
        {
            world.Set(players[index], new ClaimsComponent(index + 1, 1));
        }

        for (var index = 0; index < players.Length; index++)
        {
            Assert.AreEqual(index + 1, world.Get<ClaimsComponent>(players[index]).Account);
        }
    }
}
