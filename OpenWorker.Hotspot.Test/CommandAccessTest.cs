using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Commands;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Server.Common;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class CommandAccessTest
{
    private sealed class Counting : ICommand
    {
        public int Runs { get; private set; }

        public ValueTask<bool> TryExecute(Entity player, string[] tokens)
        {
            Runs++;

            return ValueTask.FromResult(true);
        }
    }

    /// <summary>
    /// A test command of the chat runs only for an account with the level it needs, a game master's by default: a player's
    /// command is not run, as the original ignores it. The setting of a test stand makes every account a game master.
    /// </summary>
    [TestMethod]
    public async Task ACommandAboveTheLevelOfTheAccountIsRefused()
    {
        using var world = World.Create();

        var command = new Counting();
        var commands = new CommandManager(new Dictionary<string, ICommand> { ["/give"] = command });

        var player = world.Create(new AccessComponent(AccessLevel.None));
        var master = world.Create(new AccessComponent(AccessLevel.GameMaster));

        var closed = new CommandAccess(world, Options.Create(new CommandOptions()));
        var open = new CommandAccess(world, Options.Create(new CommandOptions { OpenToEveryone = true }));

        Assert.IsFalse(await commands.TryExecute(player, "/give 1", closed.LevelOf(player)));
        Assert.AreEqual(0, command.Runs, "The command of a player is not run.");

        Assert.IsTrue(await commands.TryExecute(master, "/give 1", closed.LevelOf(master)));
        Assert.IsTrue(await commands.TryExecute(player, "/give 1", open.LevelOf(player)), "A test stand lets everybody.");
        Assert.AreEqual(2, command.Runs);
    }
}
