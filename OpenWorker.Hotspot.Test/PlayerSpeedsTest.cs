using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Boosters.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The speeds and resistances in the stats of a player, and the boosters that raise stats. The tables are made by the test.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayerSpeedsTest
{
    private const byte AttackShare = 34;
    private const byte MoveSpeedShare = 31;
    private const byte StunResistance = 70;
    private const ushort Quickness = 810;

    private static readonly ReadOnlyCollection<BuffRow> Buffs = new(
    [
        TestRows.Build<BuffRow>(
            ("Id", Quickness), ("AllowedPlaces", (byte)10), ("Kind", (byte)BuffKind.Buff), ("Effect", (ushort)BuffEffect.Stats), ("Group", Quickness), ("SuccessRate", 10000),
            ("Grade", (byte)1), ("OverlapCount", (byte)1), ("Stat1", MoveSpeedShare), ("Value1", 30), ("Stat2", StunResistance), ("Value2", 25))
    ]);

    private static ReadOnlyCollection<T> Rows<T>(params T[] rows) => new(rows);

    /// <summary>
    /// The speeds of a player are 100 plus the shares its buffs add, its resistances the numbers they add; the player is told
    /// both in the stat list, and the speed the pace of its moves allows follows.
    /// </summary>
    [TestMethod]
    public async Task TheSpeedsAndResistancesOfAPlayerAreCountedToldAndUsed()
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = world.Create(
                new ActorComponent(100, ActorType.User),
                new ServerSessionComponent(connection),
                new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
                new LevelComponent(10, 0),
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                    SoulGain = default,
                    SoulVapor = default,
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 100f, Attack = 100f }
                });

            var tables = new CombatTables(
                TestMazes.None<MonsterRow>(),
                Rows(TestRows.Build<StatusRow>(("Id", (short)1), ("MoveSpeedRate", 1f), ("AttackSpeedRate", 1f), ("HealthFirst", 100f))),
                TestMazes.None<SkillRow>(),
                Rows(TestRows.Build<CharacterInfoRow>(("Id", (short)1000), ("Character_Status", 1), ("Character_Status_Type", (byte)1))));

            var stats = new PlayerStatsService(world, tables);
            var book = new BuffBook();
            var catalog = new BuffCatalog(Buffs);

            world.Add(player, new BuffBookComponent { Book = book });
            stats.SendAll(player);

            Assert.AreEqual(100f, stats.Of(player).MoveSpeed);

            Assert.IsTrue(BuffRules.Give(book, catalog, Quickness, 100, new BuffCarrier(100, 100, 100, false), new Random(1), [], roll: false));
            stats.Send(player);

            var now = stats.Of(player);

            Assert.AreEqual(130f, now.MoveSpeed);
            Assert.AreEqual(100f, now.AttackSpeed);
            Assert.AreEqual(25f, now.Resist.Stun);
            Assert.AreEqual(130f, world.Get<AbilityComponent>(player).Speed.Move, "The pace of the moves follows the speed.");

            await MetricAssertions.UntilAsync(() => Told(connection, STAT_TYPE.STAT_MSR, 130f) && Told(connection, STAT_TYPE.STAT_RES_STUN, 25f), "The player is told both.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private static bool Told(RecordingConnection connection, STAT_TYPE stat, float value)
    {
        var opcode = new MessageOpcode(GroupOpcode.Character, (byte)CharacterOpcode.UpdateStatList);

        foreach (var frame in connection.Frames.Select(PlayerFrame.Open).Where(e => e.Opcode.Value == opcode.Value))
        {
            // u8 type, u32 actor, u8 count, then each stat as f32 value and u16 type.
            var body = frame.Body;

            for (var i = 0; i < body[5]; i++)
            {
                var at = 6 + (i * 6);

                if (BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(at + 4)) == (ushort)stat && BinaryPrimitives.ReadSingleLittleEndian(body.AsSpan(at)) == value)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// A booster of the status kind gives the stats it names for as long as the person has it.
    /// </summary>
    [TestMethod]
    public void ABoosterOfTheStatusKindRaisesTheStatsWhileThePersonHasIt()
    {
        var world = World.Create();

        try
        {
            var player = world.Create(new ActorComponent(100, ActorType.User), new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru }, new LevelComponent(10, 0));
            var catalog = BoosterCatalog.From([new BoosterDefinition(7, 7, BoosterTime.Endless, 0, [new BoosterEffectValue(BoosterEffect.Status, (BoosterApply)AttackShare, 10f)])]);
            var tables = new CombatTables(TestMazes.None<MonsterRow>(), TestMazes.None<StatusRow>(), TestMazes.None<SkillRow>(), TestMazes.None<CharacterInfoRow>(), boosters: catalog);
            var book = new BoosterBook();

            world.Add(player, new BoosterComponent { Person = 1, Book = book });

            Assert.AreEqual(0f, tables.SheetOf(world, player, Hero.Haru).Share(STAT_TYPE.STAT_PATK_MAX));

            book.Boosters.Add(new BoosterEntry { Booster = 7 });
            Assert.AreEqual(10f, tables.SheetOf(world, player, Hero.Haru).Share(STAT_TYPE.STAT_PATK_MAX));

            book.Boosters.Clear();
            Assert.AreEqual(0f, tables.SheetOf(world, player, Hero.Haru).Share(STAT_TYPE.STAT_PATK_MAX));
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
