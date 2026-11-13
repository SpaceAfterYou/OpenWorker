using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A defensive weapon of a maze: a monster whose kind makes it one. A player gets on it from near it when nobody else is on it,
/// nothing hurts the player meanwhile and its own skills wait, and everybody is told when it gets on and off.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DefensiveWeaponTest
{
    private const int Sector = 10;
    private const int Weapon = 10000206;
    private const int Plain = 20000;

    private static string Vector(Vector3 v) => string.Create(CultureInfo.InvariantCulture, $"{v.X:0.000},{v.Y:0.000},{v.Z:0.000}");

    private static string Basic(int id, Vector3 low, Vector3 high) =>
        $"""<m_ID type="0" value="{id}" /> <m_iGenerateID type="0" value="{id}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="{Vector(low)}" /> <m_vPosBottomRight type="7" value="{Vector(high)}" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="{Vector(high - low)}" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" />""";

    private static string SectorBox() =>
        $"""<VSectorBox boxtype="eEventBoxType_Sector"> {Basic(Sector, Vector3.Zero, new Vector3(100000f))} <m_eType type="4" value="General" /> <m_szEnterLua type="12" value="" /> <m_eClearType type="4" value="None" /> <m_iClearKillRatio type="0" value="0" /> <m_szClearLua type="12" value="" /> <m_iSectorTitle type="0" value="0" /> <m_iSectorExitType type="4" value="Portal" /> <m_iSectorExitID type="0" value="0" /> <m_iRelativeSectorID type="0" value="0" /> <m_iConditionKillRatio_1Step type="0" value="0" /> <m_iConditionKillRatio_2Step type="0" value="0" /> <m_iConditionKillRatio_3Step type="0" value="0" /> <m_iConditionKillRatio_4Step type="0" value="0" /> <m_iConditionKillRatio_5Step type="0" value="0" /> </VSectorBox>""";

    /// <summary>
    /// A box that brings one monster in at once, around the centre given.
    /// </summary>
    private static string SpawnBox(int id, int monster, Vector3 centre)
    {
        var text = new StringBuilder($"""<VMonsterSpawnBox boxtype="eEventBoxType_MonsterSpawn"> {Basic(id, centre - new Vector3(1f), centre + new Vector3(1f))} """);

        for (var slot = 1; slot <= 10; slot++)
        {
            var kind = slot == 1 ? monster : 0;

            text.Append(CultureInfo.InvariantCulture, $"""<m_eType{slot} type="4" value="Monster" /> <m_iMonsterID{slot} type="0" value="{kind}" /> <m_iChance{slot} type="0" value="{(kind == 0 ? 0 : 10000)}" /> """);
        }

        text.Append(CultureInfo.InvariantCulture, $"""<m_eCreationPositionType type="4" value="Center" /> <m_eMoveType type="4" value="Free" /> <m_eCreationCondition type="4" value="Immediate" /> <m_CreationEffectFile type="12" value="" /> <m_fWaitCreationDelayTime type="2" value="0.0000" /> <m_fWaitCreationSequenceTime type="2" value="0.0000" /> <m_iWaitCreationMaxWave type="0" value="1" /> <m_iMaxEntityCount type="0" value="1" /> <m_eWaitCreationSequenceType type="4" value="All" /> <m_iWaypoint type="0" value="0" /> <m_iAggroGroupID type="0" value="0" /> <m_iAggroDistance type="0" value="0" /> <m_iAggroMaxCount type="0" value="0" /> <m_fLookRatio type="2" value="1.0000" /> <m_bShowSight type="5" value="FALSE" /> <m_szObjectKey type="12" value="" /> <m_eScriptType type="4" value="None" /> <m_iCheckScriptHP1 type="0" value="0" /> <m_iCheckScriptHP2 type="0" value="0" /> <m_iCheckScriptHP3 type="0" value="0" /> <m_iCheckScriptHP4 type="0" value="0" /> <m_iCheckScriptHP5 type="0" value="0" /> <m_iSectorID type="0" value="{Sector}" /> <m_ChangeSpawnAction type="12" value="" /> <m_ProtectionTarget type="0" value="0" /> <m_RespawnTime type="2" value="0.0000" /> <m_iStep type="0" value="1" /> <m_eRespawnType type="4" value="None" /> <m_iRespawnCondition type="0" value="0" /> <m_iGroupID type="0" value="0" /> </VMonsterSpawnBox>""");

        return text.ToString();
    }

    private static string Map() =>
        "<root><Batchs eventtype=\"EventBox\">" + SectorBox() + SpawnBox(101, Weapon, new Vector3(1000f, 1000f, 0f)) + SpawnBox(102, Plain, new Vector3(1000f, 1200f, 0f)) +
        "</Batchs><Batchs eventtype=\"EventPoint\"/></root>";

    private static Entity Player(World world, RecordingConnection connection, int actor, Vector3 place)
    {
        var life = new AbilityComponentEntry { Current = 100, Max = 100 };

        return world.Create(
            new ActorComponent(actor, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = place, Rotation = 0f, Jump = 0 },
            new PersonInfoComponent { Name = "Rider", Hero = Hero.Haru },
            new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
    }

    /// <summary>
    /// The opcodes the connection was sent so far, once a notice sent after them has arrived.
    /// </summary>
    private static int[] Sent(World world, Entity player, RecordingConnection connection)
    {
        var sentinel = new ChatNoticeResponse { Message = "end" };
        var before = connection.Frames.Select(PlayerFrame.Open).Count(e => e.Opcode.Value == sentinel.Opcode.Value);

        world.Get<ServerSessionComponent>(player).Send(sentinel);

        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (connection.Frames.Select(PlayerFrame.Open).Count(e => e.Opcode.Value == sentinel.Opcode.Value) <= before && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(5);
        }

        return connection.Frames.Select(PlayerFrame.Open).Select(e => (int)e.Opcode.Value).Where(e => e != sentinel.Opcode.Value).ToArray();
    }

    /// <summary>
    /// A maze with a weapon and a plain monster near it, one player far from both and one next to the weapon; the maze runs on a
    /// clock stepped by hand.
    /// </summary>
    private sealed class Arena : IDisposable
    {
        private const int FirstSkill = 39063;
        private const int SecondSkill = 39064;

        private readonly FakeTimeProvider _time = new();
        private readonly SimulationClock _clock;

        public Arena()
        {
            var monsters = new ReadOnlyCollection<MonsterRow>([
                TestRows.Build<MonsterRow>(("Id", Weapon), ("Type", (byte)5), ("Skill1", FirstSkill), ("Skill2", SecondSkill)),
                TestRows.Build<MonsterRow>(("Id", Plain))
            ]);
            var skills = new ReadOnlyCollection<SkillRow>([
                TestRows.Build<SkillRow>(("Id", FirstSkill), ("Duration", (ushort)1000)),
                TestRows.Build<SkillRow>(("Id", SecondSkill), ("Duration", (ushort)1000))
            ]);
            var tables = new CombatTables(monsters, TestMazes.None<StatusRow>(), skills, TestMazes.None<CharacterInfoRow>());
            var batch = TestMazes.Map(Map());
            var actions = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance);

            _clock = TestMazes.Clock(_time);

            var services = new CreatureServices(new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, Path.Combine(Path.GetTempPath(), "no-such-folder")),
                new MonsterBehaviorTables(monsters, skills), actions, _clock, new NavMeshProvider());

            Rider = Player(World, Connection, 1, new Vector3(100f, 100f, 0f));
            Second = Player(World, Other, 2, new Vector3(1000f, 900f, 0f));
            Creatures = new LuaCreatureManager(World, batch, tables, services);
            Maze = TestMazes.Create(World, Rider, clock: _clock, batch: batch, creatures: Creatures, actions: actions, tables: tables);

            Maze.Join(Second);
            Maze.MarkLoaded(Rider);
            Maze.MarkLoaded(Second);
            Maze.TryStart();
            Maze.SpawnSector(Sector);

            WeaponActor = (int)World.Get<ActorComponent>(Creatures.Living.Single(e => World.Get<CreatureComponent>(e).Identifier == Weapon));
            PlainActor = (int)World.Get<ActorComponent>(Creatures.Living.Single(e => World.Get<CreatureComponent>(e).Identifier == Plain));
        }

        public World World { get; } = World.Create();
        public RecordingConnection Connection { get; } = new();
        public RecordingConnection Other { get; } = new();
        public Entity Rider { get; }
        public Entity Second { get; }
        public LuaCreatureManager Creatures { get; }
        public LuaMaze Maze { get; }
        public int WeaponActor { get; }
        public int PlainActor { get; }

        /// <summary>
        /// The rider walks up to the weapon and gets on it.
        /// </summary>
        public void Mount()
        {
            World.Set(Rider, World.Get<WorldComponent>(Rider) with { Position = new Vector3(1000f, 1300f, 0f) });

            Assert.AreEqual(ClaimVerdict.Taken, Maze.MountWeapon(Rider, WeaponActor));
        }

        public void Pass(double seconds)
        {
            for (var elapsed = 0d; elapsed < seconds; elapsed += SimulationClock.Period.TotalSeconds)
            {
                _time.Advance(SimulationClock.Period);
                _clock.Tick();
            }
        }

        /// <summary>
        /// The opcodes the second player was told so far.
        /// </summary>
        public int[] Told() => Sent(World, Second, Other);

        public void Dispose()
        {
            Maze.Close();
            World.Destroy(World);
        }
    }

    /// <summary>
    /// Only a weapon is got on, from near it, by one player at a time. The player on it cannot be hurt and plays no skill of its
    /// own; it gets off only the weapon it is on, and then it can be hurt again. Everybody hears of both.
    /// </summary>
    [TestMethod]
    public void APlayerGetsOnAWeaponFromNearItAloneAndOffItAgain()
    {
        using var arena = new Arena();
        var maze = arena.Maze;

        Assert.IsEmpty(arena.Creatures.FindMonsters((_, _) => true).Where(e => arena.World.Get<CreatureComponent>(e).Identifier == Weapon), "Nobody hits a weapon");
        Assert.AreEqual(ClaimVerdict.Refused, maze.MountWeapon(arena.Second, arena.PlainActor), "A monster that is no weapon");
        Assert.AreEqual(ClaimVerdict.Refused, maze.MountWeapon(arena.Rider, arena.WeaponActor), "From afar");

        arena.Mount();

        Assert.AreEqual(ClaimVerdict.Dropped, maze.MountWeapon(arena.Second, arena.WeaponActor), "Somebody is on it");
        Assert.IsTrue(arena.Creatures.Vitals.IsProtected(arena.Rider));
        Assert.IsFalse(maze.MayStartSkill(arena.Rider));

        Assert.AreEqual(ClaimVerdict.Dropped, maze.DismountWeapon(arena.Second, arena.WeaponActor), "Not on it");
        Assert.AreEqual(ClaimVerdict.Taken, maze.DismountWeapon(arena.Rider, arena.WeaponActor));
        Assert.IsFalse(arena.Creatures.Vitals.IsProtected(arena.Rider));
        Assert.IsTrue(maze.MayStartSkill(arena.Rider));

        var mount = (int)new MonsterWeaponMountResponse().Opcode.Value;
        var dismount = (int)new MonsterWeaponDismountResponse().Opcode.Value;

        CollectionAssert.AreEqual(new[] { mount, dismount }, arena.Told().Where(e => e == mount || e == dismount).ToArray());
    }

    /// <summary>
    /// Only the player on a weapon fires it, a skill its row has, and not again before the last one is over; everybody sees the
    /// weapon play the skill.
    /// </summary>
    [TestMethod]
    public void OnlyTheRiderFiresItsWeaponOneSkillAtATime()
    {
        using var arena = new Arena();
        var maze = arena.Maze;

        Assert.AreEqual(ClaimVerdict.Dropped, maze.FireWeapon(arena.Rider, arena.WeaponActor, 0), "Not on it");

        arena.Mount();

        Assert.AreEqual(ClaimVerdict.Dropped, maze.FireWeapon(arena.Second, arena.WeaponActor, 0), "Somebody else's");
        Assert.AreEqual(ClaimVerdict.Refused, maze.FireWeapon(arena.Rider, arena.WeaponActor, 2), "The row has no third skill");
        Assert.AreEqual(ClaimVerdict.Taken, maze.FireWeapon(arena.Rider, arena.WeaponActor, 0));
        Assert.AreEqual(ClaimVerdict.Dropped, maze.FireWeapon(arena.Rider, arena.WeaponActor, 1), "The first still plays");

        arena.Pass(1.1);

        Assert.AreEqual(ClaimVerdict.Taken, maze.FireWeapon(arena.Rider, arena.WeaponActor, 1));

        var active = (int)new SkillActiveBroadcastResponse().Opcode.Value;

        Assert.HasCount(2, arena.Told().Where(e => e == active));
    }
}
