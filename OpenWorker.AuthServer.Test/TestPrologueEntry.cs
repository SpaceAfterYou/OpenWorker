using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.GateServer.Gameplay;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// The gate sends a selected character to the prologue only when it has the prologue ahead and the server plays it.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TestPrologueEntry
{
    private sealed class Flags : IPrologueStore
    {
        public HashSet<int> Pending { get; } = [];

        public Task<bool> IsPendingAsync(int person, int account, short gate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Pending.Contains(person));
        }

        public Task<bool> FinishAsync(int person, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Pending.Remove(person));
        }
    }

    private static Entity Person(World world, int identifier) => world.Create(
        new ActorComponent { Identifier = identifier, Type = ActorType.User },
        new PersonInfoComponent { Name = "Tester", Hero = Hero.Haru });

    [TestMethod]
    public async Task OnlyACharacterWithThePrologueAheadIsSentToIt()
    {
        var world = World.Create();

        try
        {
            const int fresh = 1, old = 2, stranger = 3;

            var flags = new Flags();
            flags.Pending.UnionWith([fresh, stranger]);

            var player = world.Create(new ClaimsComponent(7, 0), new GatePersonComponent([Person(world, fresh), Person(world, old)]));

            PrologueEntry Entry(bool prologue) => new(
                world,
                Options.Create(new GameOptions { Version = "1.0", Prologue = prologue }),
                Options.Create(new GateOptions { Id = 1 }),
                Options.Create(new MazeEndpointOptions()),
                flags,
                null!,
                new ReadOnlyCollection<CharacterStartRow>([]),
                null!,
                null!,
                NullLogger<PrologueEntry>.Instance);

            Assert.IsTrue(await Entry(true).IsAheadAsync(player, fresh));
            Assert.IsFalse(await Entry(true).IsAheadAsync(player, old), "a character that has played it goes to its district");
            Assert.IsFalse(await Entry(true).IsAheadAsync(player, stranger), "a character the account does not list is left to the selection");
            Assert.IsFalse(await Entry(false).IsAheadAsync(player, fresh), "with the prologue off nobody sees it");
        }

        finally
        {
            World.Destroy(world);
        }
    }

}
