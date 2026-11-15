using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The soul vapor of a player. The tables are made by the test.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayerSoulVaporTest
{
    /// <summary>
    /// A player enters with no soul vapor and the size its level gives; what it gets fills up to the size, a skill spends it,
    /// a smaller size takes the rest away, and the player is told every change.
    /// </summary>
    [TestMethod]
    public async Task SoulVaporStartsEmptyFillsUpToItsSizeAndIsSpent()
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = world.Create(
                new ActorComponent(100, ActorType.User),
                new ServerSessionComponent(connection),
                new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
                new LevelComponent(30, 0),
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                    SoulGain = default,
                    SoulVapor = new AbilityComponentEntry { Current = 120, Max = 200 },
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 100f, Attack = 100f }
                });
            var levels = new ReadOnlyCollection<LevelupPointRow>(
            [
                TestRows.Build<LevelupPointRow>(("Id", (byte)29), ("SoulVaporMax", (short)50)),
                TestRows.Build<LevelupPointRow>(("Id", (byte)30), ("SoulVaporMax", (short)200))
            ]);
            var tables = new CombatTables(TestMazes.None<MonsterRow>(), TestMazes.None<StatusRow>(), TestMazes.None<SkillRow>(), TestMazes.None<CharacterInfoRow>(), levels: levels);
            var stats = new PlayerStatsService(world, tables);
            var gauges = new PlayerGauges(world, tables);

            stats.SendAll(player);

            Assert.AreEqual(new AbilityComponentEntry { Current = 0, Max = 200 }, world.Get<AbilityComponent>(player).SoulVapor, "every entry begins empty");
            await MetricAssertions.UntilAsync(() => PlayerSpeedsTest.Told(connection, STAT_TYPE.STAT_SV_MAX, 200f) && PlayerSpeedsTest.Told(connection, STAT_TYPE.STAT_SV_CUR, 0f), "The size is told.");

            PlayerSoulVapor.Gain(world, player, 150);
            PlayerSoulVapor.Gain(world, player, 100);

            Assert.AreEqual(200, PlayerSoulVapor.Of(world, player), "no more than the size");
            Assert.IsFalse(gauges.CanPay(player, SkillCostKind.SoulVapor, 250f));

            gauges.Pay(player, SkillCostKind.SoulVapor, 100f);

            Assert.AreEqual(100, PlayerSoulVapor.Of(world, player));

            world.Set(player, new LevelComponent(29, 0));
            stats.Send(player);

            Assert.AreEqual(new AbilityComponentEntry { Current = 50, Max = 50 }, world.Get<AbilityComponent>(player).SoulVapor);
            await MetricAssertions.UntilAsync(
                () => PlayerSpeedsTest.Told(connection, STAT_TYPE.STAT_SV_CUR, 150f) && PlayerSpeedsTest.Told(connection, STAT_TYPE.STAT_SV_CUR, 100f) && PlayerSpeedsTest.Told(connection, STAT_TYPE.STAT_SV_CUR, 50f),
                "Every change is told.");
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
