using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Lua;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;
using static OpenWorker.Hotspot.Test.MazeRuleRows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Who the revive items of a maze are for: the call gets up its user, the call of a party the fallen of the party in the
/// maze, a revive buff or its aura is refused to whoever carries one already. A maze that lets nobody get up refuses them all.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeReviveItemsTest
{
    private const ushort Revive = 10002;

    private static readonly ReadOnlyCollection<BuffRow> Buffs = new(
    [
        TestRows.Build<BuffRow>(("Id", Revive), ("AllowedPlaces", 10), ("Kind", (byte)BuffKind.Buff), ("Effect", (ushort)BuffEffect.Revive),
            ("Group", Revive), ("SuccessRate", 10000), ("Grade", 1), ("DurationMilliseconds", 600000), ("OverlapCount", 1))
    ]);

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();

        public Entity First { get; }

        public Entity Second { get; }

        public LuaMaze Maze { get; }

        public MazeReviveItems Items { get; }

        public MazeReviveItems Nowhere { get; }

        public Fixture(int party)
        {
            First = Player(1);
            Second = Player(2);

            Maze = TestMazes.Create(World, First, party: party, buffs: new BuffManager(Buffs));

            World.Add(First, Maze);

            if (party != 0)
            {
                Maze.Join(Second);
                World.Add(Second, Maze);
            }

            Items = new MazeReviveItems(World, Revival(Rule(0, revivalItems: 2)));
            Nowhere = new MazeReviveItems(World, Revival(Rule(0, type: 7, revivalType: 1)));
        }

        public void Fall(Entity player)
        {
            var ability = World.Get<AbilityComponent>(player);

            World.Set(player, ability with { Health = ability.Health with { Current = 0 } });
        }

        public bool Standing(Entity player) => Maze.Creatures.Vitals.IsAlive(player);

        public void CarryRevive(Entity player) => Assert.IsTrue(Maze.Buffs.Give(player, Revive, player));

        private MazeRevival Revival(MazeRuleRow rule)
        {
            var rules = Rules(rule);
            var limits = new MazeEntryLimits(
                World,
                new PersonStateStore(null!),
                rules,
                new ReadOnlyCollection<MazeDailyPortalRow>([]),
                new ReadOnlyCollection<MazeOpenControlRow>([]),
                new FakeTimeProvider(),
                Options.Create(new DailyMissionOptions()),
                NullLogger<MazeEntryLimits>.Instance);

            return new MazeRevival(rules, limits);
        }

        private Entity Player(int actor)
        {
            return World.Create(
                new ActorComponent(actor, ActorType.User),
                new ServerSessionComponent(new RecordingConnection()),
                new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
                new LevelComponent(10, 0),
                new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                    SoulGain = default,
                    SoulVapor = default,
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
                });
        }

        public void Dispose() => World.Destroy(World);
    }

    [TestMethod]
    public void TheCallOfAPartyGetsItsFallenUpAndTheReviveBuffsAreForThoseWhoCarryNone()
    {
        using var f = new Fixture(party: 5);

        foreach (var kind in Enum.GetValues<ReviveItemKind>())
        {
            Assert.AreEqual(ItemUseCodes.NoRevive, f.Nowhere.Check(f.First, kind), $"{kind}: nobody gets up in this maze");
        }

        Assert.AreEqual(MazeRevival.NotFallen, f.Items.Check(f.First, ReviveItemKind.Call), "the call is for a fallen user");
        Assert.AreEqual(MazeReviveItems.NobodyInParty, f.Items.Check(f.First, ReviveItemKind.PartyCall), "nobody of the party has fallen");

        f.Fall(f.Second);

        Assert.IsNull(f.Items.Check(f.First, ReviveItemKind.PartyCall), "a standing user calls up a fallen member");

        f.Items.GetUp(f.First, ReviveItemKind.PartyCall);

        Assert.IsTrue(f.Standing(f.Second), "the fallen member gets up");

        f.Fall(f.First);
        f.Fall(f.Second);

        Assert.IsNull(f.Items.Check(f.First, ReviveItemKind.Call));

        f.Items.GetUp(f.First, ReviveItemKind.Call);

        Assert.IsTrue(f.Standing(f.First), "the call gets its user up");
        Assert.IsFalse(f.Standing(f.Second), "and nobody else");

        // A fallen player takes no buff: the member is called up first.
        f.Items.GetUp(f.First, ReviveItemKind.PartyCall);

        Assert.IsNull(f.Items.Check(f.First, ReviveItemKind.Buff));

        f.CarryRevive(f.First);

        Assert.AreEqual(MazeReviveItems.AlreadyRevivable, f.Items.Check(f.First, ReviveItemKind.Buff), "the user carries one already");
        Assert.IsNull(f.Items.Check(f.First, ReviveItemKind.PartyBuff), "a member carries none");

        f.CarryRevive(f.Second);

        Assert.AreEqual(MazeReviveItems.NobodyInParty, f.Items.Check(f.First, ReviveItemKind.PartyBuff), "every member carries one");
    }

    [TestMethod]
    public void AloneTheItemsOfAPartyAreForTheUserOnly()
    {
        using var f = new Fixture(party: 0);

        Assert.AreEqual(MazeRevival.NotFallen, f.Items.Check(f.First, ReviveItemKind.PartyCall), "the call of a party alone is a call");
        Assert.IsNull(f.Items.Check(f.First, ReviveItemKind.PartyBuff));

        f.CarryRevive(f.First);

        Assert.AreEqual(MazeReviveItems.AlreadyRevivable, f.Items.Check(f.First, ReviveItemKind.PartyBuff), "the aura alone is the buff");

        f.Fall(f.First);

        Assert.IsNull(f.Items.Check(f.First, ReviveItemKind.PartyCall));

        f.Items.GetUp(f.First, ReviveItemKind.PartyCall);

        Assert.IsTrue(f.Standing(f.First));
    }
}
