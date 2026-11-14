using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Lua;
using Lua.Standard;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Lua;
using OpenWorker.Lua.Enums;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The prologue of a new character in the maze: the way out of the tutorial ends it once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PrologueTest
{
    private const short Tutorial = 21112;

    /// <summary>
    /// The prologue flags of the characters, as the database would hold them.
    /// </summary>
    private sealed class Flags : IPrologueStore
    {
        public HashSet<int> Pending { get; } = [];

        public int Finished { get; private set; }

        public Task<bool> IsPendingAsync(int person, int account, short gate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Pending.Contains(person));
        }

        public Task<bool> FinishAsync(int person, CancellationToken cancellationToken = default)
        {
            Finished++;

            return Task.FromResult(Pending.Remove(person));
        }
    }

    private static Entity Person(World world, int identifier) => world.Create(
        new ActorComponent { Identifier = identifier, Type = ActorType.User },
        new PersonInfoComponent { Name = "Tester", Hero = Hero.Haru });

    /// <summary>
    /// A row of the maze table with an id and a type, the rest empty.
    /// </summary>
    private static MazeInfoRow Maze(short id, byte type)
    {
        var bytes = new byte[4096];

        BitConverter.TryWriteBytes(bytes, id);
        bytes[2] = type;

        return new MazeInfoRow(new BinaryReader(new MemoryStream(bytes)));
    }

    [TestMethod]
    public async Task LeavingTheTutorialEndsThePrologueOnce()
    {
        var world = World.Create();

        try
        {
            var flags = new Flags();
            flags.Pending.Add(5);

            var prologue = new PrologueService(world, flags, TestMazes.Quests(world), NoPersonFlush.Instance,
                new ReadOnlyCollection<MazeInfoRow>([Maze(Tutorial, PrologueService.TutorialMaze), Maze(21311, 0)]),
                new PrologueQuests(new ReadOnlyCollection<CharacterStartRow>([])),
                NullLogger<PrologueService>.Instance);

            var player = Person(world, 5);
            var row = new PersonPersistent
            {
                Name = "Tester", Hero = Hero.Haru, DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0, Location = Tutorial,
                PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0, Account = null!, Gate = null!,
                ProloguePending = true
            };

            world.Add(player, new WorldComponent { Location = 21311, Position = Vector3.Zero, Rotation = 0f, Jump = 0 });
            prologue.Enter(player, row);

            // Out of a maze that is no tutorial, the prologue goes on.
            await prologue.LeaveAsync(player);
            Assert.AreEqual(0, flags.Finished);

            world.Set(player, new WorldComponent { Location = Tutorial, Position = Vector3.Zero, Rotation = 0f, Jump = 0 });

            await prologue.LeaveAsync(player);
            await prologue.LeaveAsync(player);

            Assert.AreEqual(1, flags.Finished, "the flag is written once");
            Assert.IsEmpty(flags.Pending);

            // A character that played it, back in the tutorial through a portal, has nothing to end.
            var veteran = Person(world, 6);

            world.Add(veteran, new WorldComponent { Location = Tutorial, Position = Vector3.Zero, Rotation = 0f, Jump = 0 });
            row.ProloguePending = false;
            prologue.Enter(veteran, row);

            await prologue.LeaveAsync(veteran);
            Assert.AreEqual(1, flags.Finished);
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
