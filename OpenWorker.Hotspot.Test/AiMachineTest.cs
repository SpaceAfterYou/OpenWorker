using System.Numerics;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed partial class AiMachineTest
{
    private const float Step = 0.1f;

    /// <summary>
    /// A monster that walks in a straight line and a player who stands still.
    /// </summary>
    private sealed class Host : IAiHost
    {
        public Vector3 Position { get; set; }
        public Vector3 HomePosition { get; set; }
        public float Yaw { get; set; }
        public float Radius => 40f;
        public int Health { get; set; } = 100;
        public int MaxHealth => 100;
        public bool IsDead { get; set; }
        public bool CanMove => !IsAttacking;
        public bool CanAttack => !IsAttacking;
        public bool IsMoving => Destination is not null;
        public bool IsRooted { get; set; }
        public bool IsAttacking { get; set; }
        public bool IsBattlePose { get; set; }
        public float Age { get; set; }
        public float Speed { get; set; } = 300f;
        public float AttackLength { get; set; } = 1f;
        public Vector3? Destination { get; set; }
        public AiTarget? Player { get; set; }

        /// <summary>
        /// Other players in the fight, beside <see cref="Player"/>.
        /// </summary>
        public List<AiTarget> Others { get; } = [];

        public bool Down { get; set; }
        public List<(AiDefense Defense, float Seconds)> Forced { get; } = [];
        public List<float> Escapes { get; } = [];
        public List<int> Alerted { get; } = [];
        public Dictionary<int, AiSkill> Known { get; } = [];
        public AiSkill?[] Skills { get; } = new AiSkill?[AiProfile.SkillSlots];
        public List<string> Log { get; } = [];
        public List<AiState> Visited { get; } = [];
        public List<int> Used { get; } = [];
        public int Target { get; private set; } = AiMachine.NoTarget;
        public float PlayLength { get; set; } = 1f;
        public bool Gazing { get; private set; }
        public bool Invincible { get; private set; }
        public bool Restored { get; private set; }

        private float _attackLeft;

        public AiSkill? GetSkill(int slot) => Skills[slot];

        public AiTarget? GetTarget(int id)
        {
            if (Player is { } player && player.Id == id)
            {
                return player;
            }

            foreach (var other in Others)
            {
                if (other.Id == id)
                {
                    return other;
                }
            }

            return null;
        }

        public AiSkill? FindSkill(int id) => Known.GetValueOrDefault(id);
        public bool IsDownOrFlying => Down;
        public bool IsArmorBroken { get; set; }
        public void ForceDefense(AiDefense defense, float seconds) => Forced.Add((defense, seconds));
        public void NotifyEscape(float seconds) => Escapes.Add(seconds);
        public void AlertGroup(int target) => Alerted.Add(target);

        public AiTarget? FindEnemy(float distance)
        {
            return Player is { } player && Vector3.Distance(player.Position, Position) - player.Radius <= distance ? player : null;
        }

        public int CountEnemies(float distance) => FindEnemy(distance) is null ? 0 : 1;
        public int CountSectorMonsters() => 1;
        public int CountSpawnedMonsters() => 0;

        public void StopMoving(bool send)
        {
            Destination = null;
            Log.Add("stop");
        }

        /// <summary>
        /// No way can be found to anywhere.
        /// </summary>
        public bool NoWay { get; set; }

        public bool MoveTo(Vector3 destination, bool run)
        {
            if (NoWay)
            {
                return false;
            }

            Destination = destination;
            Log.Add(run ? "run" : "walk");

            return true;
        }

        public void FaceTo(Vector3 point)
        {
            Yaw = AiMath.YawTo(Position, point);
        }

        public void SetBattlePose(bool battle, bool playMotion)
        {
            IsBattlePose = battle;
        }

        public void SetTarget(int id)
        {
            Target = id;
            Log.Add($"target {id}");
        }

        public void SetGaze(bool gazing)
        {
            Gazing = gazing;
        }

        public bool UseSkill(AiSkill skill, AiTarget target)
        {
            Used.Add(skill.Id);
            IsAttacking = true;
            _attackLeft = AttackLength;

            return true;
        }

        public float PlayAction(string name)
        {
            Log.Add("action " + name);

            return PlayLength;
        }

        public void SetInvincible(bool invincible) => Invincible = invincible;

        public void ClearBuffs() { }

        public void RestoreHealth()
        {
            Restored = true;
            Health = MaxHealth;
        }

        public void Teleport(Vector3 position)
        {
            Log.Add("teleport");
            Position = position;
            Destination = null;
        }

        public void Die()
        {
            IsDead = true;
        }

        public void Advance(float seconds)
        {
            Age += seconds;

            if (IsAttacking && (_attackLeft -= seconds) <= 0f)
            {
                IsAttacking = false;
            }

            if (Destination is { } destination)
            {
                var offset = destination - Position;
                var distance = offset.Length();

                if (distance <= Speed * seconds)
                {
                    Position = destination;
                    Destination = null;
                }
                else
                {
                    Position += offset / distance * Speed * seconds;
                }
            }
        }
    }

    private static AiSkill Skill(int id, float min = 0, float max = 200, float cooldown = 0, int group = 0, float global = 0)
    {
        return new AiSkill(id, min, max, cooldown, group == 0 ? id : group, global, 45f, false, false);
    }

    private static AiTarget Player(Vector3 position) => new(1000, position, 40f, 0f, 100, 100, false);

    private static AiMachine Create(AiProfileBuilder builder, Host host, int seed = 1)
    {
        var machine = new AiMachine(builder.Build(), host, new Random(seed));

        machine.StateChanged += (_, state) => host.Visited.Add(state);

        return machine;
    }

    private static void Run(AiMachine machine, Host host, float seconds)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += Step)
        {
            host.Advance(Step);
            machine.Update(Step);
        }
    }

    private static AiProfileBuilder Fighter()
    {
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(0.1f);
        builder.SetFindEnemyInfo(true, 0.1f, 1000f);
        builder.RegisterStateFunctions(AiState.Wait);
        builder.RegisterStateVars(AiState.SelectAction, AiStateVariable.LifeTime, 200, 200);
        builder.RegisterStateVars(AiState.SelectAction, AiStateVariable.NextState, (int)AiState.Attack, 100);
        builder.RegisterStateVars(AiState.Attack, AiStateVariable.LifeTime, 500, 600);
        builder.RegisterConditions(AiState.Attack, AiState.AttackSkill1, AiVariable.Random, "<", 10000);
        builder.RegisterStateFunctions(AiState.AttackSkill1);
        builder.RegisterStateFunctions(AiState.SelectAction);

        return builder;
    }

    [TestMethod]
    public void AMonsterWithoutStatesStandsStill()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };
        var machine = Create(new AiProfileBuilder(), host);

        machine.Start();
        Run(machine, host, 20f);

        Assert.AreEqual(AiState.Wait, machine.State);
        Assert.AreEqual(AiMachine.NoTarget, machine.TargetId);
    }

    [TestMethod]
    public void WaitingLastsFourToFiveSecondsWhateverTheScriptSays()
    {
        var builder = new AiProfileBuilder();

        builder.RegisterStateVars(AiState.Wait, AiStateVariable.LifeTime, 100, 200);

        var settings = builder.Build().GetSettings(AiState.Wait);

        Assert.AreEqual(4000, settings.LifeMin);
        Assert.AreEqual(5000, settings.LifeMax);
    }

    [TestMethod]
    public void AnEnemyInSightIsFoughtUntilTheSkillHasBeenUsed()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };

        host.Skills[0] = Skill(11);

        var machine = Create(Fighter(), host);

        machine.Start();
        Run(machine, host, 3f);

        Assert.AreEqual(1000, host.Target);
        CollectionAssert.Contains(host.Used, 11);
        Assert.IsTrue(host.IsBattlePose);
    }

    [TestMethod]
    public void ASkillStartsOnlyOnceTheTargetIsInReach()
    {
        var host = new Host { Player = Player(new Vector3(900, 0, 0)) };

        host.Skills[0] = Skill(11, max: 150);

        var machine = Create(Fighter(), host);

        machine.Start();
        Run(machine, host, 4f);

        Assert.IsNotEmpty(host.Used);
        Assert.IsLessThan(200f, Vector3.Distance(host.Position, host.Player!.Value.Position));
    }

    /// <summary>
    /// A target a little above or below is in reach as if it stood level: the height between the two is taken off the distance.
    /// </summary>
    [TestMethod]
    public void TheReachOfASkillTakesOffTheHeightOfATargetOnNearlyOneLevel()
    {
        var host = new Host { Player = Player(new Vector3(150, 0, 150)), IsRooted = true };

        host.Skills[0] = Skill(11, max: 150);

        var machine = Create(Fighter(), host);

        machine.Start();
        Run(machine, host, 2f);

        Assert.IsNotEmpty(host.Used, "212 away and 150 above is 62 for the reach.");
    }

    [TestMethod]
    public void AnEnemyBeyondSightIsLeftAlone()
    {
        var host = new Host { Player = Player(new Vector3(5000, 0, 0)) };
        var machine = Create(Fighter(), host);

        machine.Start();
        Run(machine, host, 5f);

        Assert.AreEqual(AiMachine.NoTarget, host.Target);
        Assert.IsEmpty(host.Used);
    }

    [TestMethod]
    public void AMonsterThatDoesNotSeekEnemiesWaitsToBeHit()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };
        var builder = Fighter();

        builder.SetFindEnemyInfo(false, 0.1f, 1000f);
        host.Skills[0] = Skill(11);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 3f);

        Assert.AreEqual(AiMachine.NoTarget, host.Target);

        machine.OnDamaged(1000, 1f);
        Run(machine, host, 3f);

        Assert.AreEqual(1000, host.Target);
        Assert.IsNotEmpty(host.Used);
    }

    [TestMethod]
    public void TheHighestAggroIsTheTarget()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };
        var builder = Fighter();

        builder.SetFindEnemyInfo(false, 0.1f, 1000f);

        var machine = Create(builder, host);

        machine.Start();
        machine.OnDamaged(1000, 1f);
        Run(machine, host, 0.5f);

        Assert.AreEqual(1000, machine.TargetId);
    }

    [TestMethod]
    public void AnUnknownStateCannotBeEntered()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };
        var builder = Fighter();

        builder.RegisterConditions(AiState.Wait, AiState.Gaze, AiVariable.Random, "<", 10000);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 1f);

        Assert.AreNotEqual(AiState.Gaze, machine.State);
    }

    [TestMethod]
    public void TheFirstTransitionWhoseConditionsHoldIsTaken()
    {
        var host = new Host();
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(0.1f);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.Health, "<", 50);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction2, AiVariable.Health, "<", 80);
        builder.RegisterStateFunctions(AiState.CommonAction1);
        builder.RegisterStateFunctions(AiState.CommonAction2);

        host.Health = 70;

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 0.5f);

        Assert.AreEqual(AiState.CommonAction2, host.Visited.First());
    }

    [TestMethod]
    public void ConditionsOfTheSameTransitionMustAllHold()
    {
        var host = new Host();
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(0.1f);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.Health, "<", 80);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.IsTarget, "=", 1);
        builder.RegisterStateFunctions(AiState.CommonAction1);

        host.Health = 50;

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 0.5f);

        Assert.AreEqual(AiState.Wait, machine.State, "There is no target yet.");
    }

    [TestMethod]
    public void TheSecondIndexMakesAnotherWayToTheSameState()
    {
        var host = new Host { Health = 50 };
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(0.1f);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.IsTarget, "=", 1);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.Health, "<", 80, 0, 2);
        builder.RegisterStateFunctions(AiState.CommonAction1);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 0.5f);

        Assert.AreEqual(AiState.CommonAction1, host.Visited.First());
    }

    [TestMethod]
    public void ARangeIncludesItsStartAndExcludesItsEnd()
    {
        var condition = new AiCondition(AiVariable.TargetDistance, AiOperator.InRange, 300, 400, false);

        Assert.IsTrue(condition.IsMet(300, 300));
        Assert.IsTrue(condition.IsMet(399.9, 300));
        Assert.IsFalse(condition.IsMet(400, 300));
        Assert.IsFalse(condition.IsMet(299, 300));
    }

    [TestMethod]
    public void WholeNumbersAreCutBeforeTheyAreCompared()
    {
        var condition = new AiCondition(AiVariable.Health, AiOperator.Less, 50, 0, false);

        Assert.IsTrue(condition.IsMet(49.9, 50));
        Assert.IsFalse(condition.IsMet(50.9, 50));
    }

    [TestMethod]
    public void AnArgumentIsNotARange()
    {
        var builder = new AiProfileBuilder();

        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.GroupCooldown, "=", 1, 3);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction2, AiVariable.Random, "<", 1000, 5000);

        var transitions = builder.Build().GetTransitions(AiState.Wait);

        Assert.IsFalse(transitions[0].Conditions[0].IsRandom);
        Assert.IsTrue(transitions[1].Conditions[0].IsRandom);
    }

    [TestMethod]
    public void AMonsterFarFromHomeGoesBackAndHeals()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)), Health = 10 };
        var builder = Fighter();

        builder.SetReturnDistance(500f);
        builder.RegisterStateFunctions(AiState.Return);

        host.Skills[0] = Skill(11);
        host.Position = new Vector3(2000, 0, 0);
        host.HomePosition = Vector3.Zero;

        var machine = Create(builder, host);

        machine.Start(AiState.SelectAction);
        machine.OnDamaged(1000, 1f);
        Run(machine, host, 15f);

        Assert.IsTrue(host.Restored);
        Assert.IsFalse(host.Invincible);
        Assert.IsLessThan(200f, Vector3.Distance(host.Position, host.HomePosition));
    }

    [TestMethod]
    public void AMonsterThatComesHomeStopsNearItAndWaits()
    {
        var host = new Host { Player = Player(new Vector3(9000, 0, 0)) };
        var builder = Fighter();

        builder.SetReturnDistance(500f);
        builder.RegisterStateFunctions(AiState.Return);

        host.Position = new Vector3(2000, 0, 0);
        host.HomePosition = Vector3.Zero;

        var machine = Create(builder, host);

        machine.Start(AiState.Return);
        Run(machine, host, 10f);

        Assert.AreEqual(AiState.Wait, machine.State);
        Assert.DoesNotContain("teleport", host.Log);
    }

    [TestMethod]
    public void AMonsterWithNoWayHomeKeepsLookingAndIsPutThereWhenTheTimeIsUp()
    {
        var host = new Host { Player = Player(new Vector3(9000, 0, 0)), NoWay = true };
        var builder = Fighter();

        builder.SetReturnDistance(500f);
        builder.RegisterStateFunctions(AiState.Return);

        host.Position = new Vector3(2000, 0, 0);
        host.HomePosition = Vector3.Zero;

        var machine = Create(builder, host);

        machine.Start(AiState.Return);
        Run(machine, host, 20f);

        Assert.AreEqual(AiState.Return, machine.State);

        Run(machine, host, 15f);

        Assert.Contains("teleport", host.Log);
        Assert.AreEqual(Vector3.Zero, host.Position);
    }

    [TestMethod]
    public void ATimerConditionWaitsOnItsOwnTransition()
    {
        var host = new Host();
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(0.1f);
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.Timer, ">", 1.0);
        builder.RegisterStateFunctions(AiState.CommonAction1);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 0.7f);

        Assert.IsEmpty(host.Visited);

        Run(machine, host, 0.8f);

        Assert.AreEqual(AiState.CommonAction1, host.Visited.First());
    }

    [TestMethod]
    public void ACommonActionPlaysAndThenTheMonsterGoesBackToWaiting()
    {
        var host = new Host { PlayLength = 1.5f };
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(0.1f);
        builder.SetCommonAction(0, "B_Idle_01");
        builder.RegisterConditions(AiState.Wait, AiState.CommonAction1, AiVariable.ActivateTime, "<", 1.0);
        builder.RegisterStateFunctions(AiState.CommonAction1);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 0.5f);

        CollectionAssert.Contains(host.Log, "action B_Idle_01");
        Assert.AreEqual(AiState.CommonAction1, machine.State);

        Run(machine, host, 2f);

        Assert.AreEqual(AiState.Wait, machine.State);
        CollectionAssert.AreEqual(new[] { AiState.CommonAction1, AiState.Wait }, host.Visited);
    }

    [TestMethod]
    public void SkillGroupsPickTheSkillThatIsReady()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };
        var builder = Fighter();

        builder.SetSkillGroupInfo(0, 0, 1, 2, 0);
        builder.RegisterSkillConditions(1, AiVariable.Random, "<", 0, 0, 0);

        host.Skills[0] = Skill(11);
        host.Skills[1] = Skill(22);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 3f);

        Assert.IsNotEmpty(host.Used);
        Assert.AreEqual(22, host.Used[0], "The first skill is refused by its condition.");
    }

    [TestMethod]
    public void ASkillOnCooldownIsNotUsedAgainAtOnce()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)), AttackLength = 0.3f };
        var builder = Fighter();

        builder.SetSkillGroupInfo(0, 0, 1, 2, 0);

        host.Skills[0] = Skill(11, cooldown: 60f);
        host.Skills[1] = Skill(22);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 6f);

        Assert.AreEqual(11, host.Used[0]);
        Assert.IsTrue(host.Used.Skip(1).All(e => e == 22), "While 11 is cooling the second skill is used.");
    }

    [TestMethod]
    public void ActionAfterASkillSendsTheMonsterWhereTheScriptSays()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };
        var builder = Fighter();

        builder.RegisterActionAfterSkill(1, AiState.CommonAction1, AiVariable.Random, "<", 10000, 0, 0);
        builder.RegisterStateFunctions(AiState.CommonAction1);
        builder.SetSkillGroupInfo(0, 0, 1, 0, 0);

        host.Skills[0] = Skill(11);

        var machine = Create(builder, host);

        machine.Start();

        var seen = false;

        for (var elapsed = 0f; elapsed < 5f && !seen; elapsed += Step)
        {
            host.Advance(Step);
            machine.Update(Step);
            seen = machine.State == AiState.CommonAction1;
        }

        Assert.IsTrue(seen);
    }

    [TestMethod]
    public void SuicideKillsTheMonster()
    {
        var host = new Host();
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(0.1f);
        builder.RegisterConditions(AiState.Wait, AiState.Suicide, AiVariable.ActivateTime, ">", 0.3);
        builder.RegisterStateFunctions(AiState.Suicide);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 1f);

        Assert.IsTrue(host.IsDead);
    }

    [TestMethod]
    public void BattleMoveGoesToAPlaceAroundTheTarget()
    {
        var host = new Host { Player = Player(new Vector3(1000, 0, 0)) };
        var builder = Fighter();

        builder.RegisterStateVars(AiState.SelectAction, AiStateVariable.NextState, (int)AiState.BattleMove, 100);
        builder.RegisterStateVars(AiState.SelectAction, AiStateVariable.NextState, (int)AiState.Attack, 0);
        builder.RegisterStateVars(AiState.BattleMove, AiStateVariable.TargetDistance, 300, 300);
        builder.RegisterStateVars(AiState.BattleMove, AiStateVariable.Angle, 0, 0);
        builder.RegisterStateFunctions(AiState.BattleMove);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 4f);

        CollectionAssert.Contains(host.Log, "run");
        Assert.AreEqual(300f, Vector3.Distance(host.Position, host.Player!.Value.Position), 1f);
    }

    [TestMethod]
    public void ARootedMonsterNeverWalksAndGivesUpOnAnEnemyOutOfReach()
    {
        var host = new Host { Player = Player(new Vector3(900, 0, 0)), IsRooted = true };
        var builder = Fighter();

        host.Skills[0] = Skill(11, max: 150);

        var machine = Create(builder, host);

        machine.Start();
        Run(machine, host, 12f);

        Assert.DoesNotContain("walk", host.Log);
        Assert.DoesNotContain("run", host.Log);
        Assert.IsEmpty(host.Used);
        Assert.AreEqual(Vector3.Zero, host.Position);
    }

    [TestMethod]
    public void SelectingAnActionFollowsTheWeightsOfTheScript()
    {
        var attacks = 0;

        for (var seed = 0; seed < 200; seed++)
        {
            var host = new Host { Player = Player(new Vector3(100, 0, 0)) };
            var builder = Fighter();

            builder.RegisterStateVars(AiState.SelectAction, AiStateVariable.NextState, (int)AiState.Attack, 90);
            builder.RegisterStateVars(AiState.SelectAction, AiStateVariable.NextState, (int)AiState.BattleWait, 10);
            builder.RegisterStateFunctions(AiState.BattleWait);

            var machine = Create(builder, host, seed);

            machine.Start(AiState.SelectAction);
            machine.OnDamaged(1000, 1f);
            Run(machine, host, 0.5f);

            if (host.Visited.Contains(AiState.Attack))
            {
                attacks++;
            }
        }

        Assert.IsGreaterThan(150, attacks);
        Assert.IsLessThan(200, attacks);
    }

    [TestMethod]
    public void AStateWithoutWeightsGoesWhereItLeadsByDefault()
    {
        var builder = new AiProfileBuilder();

        Assert.AreEqual(AiState.Wait, builder.Build().GetSettings(AiState.Patrol).DefaultNext);
        Assert.AreEqual(AiState.SelectAction, builder.Build().GetSettings(AiState.BattleMove).DefaultNext);
        Assert.AreEqual(AiState.AttackSkill1, builder.Build().GetSettings(AiState.Attack).DefaultNext);
    }

    [TestMethod]
    public void EveryMonsterStartsWithTheBasicStates()
    {
        var profile = new AiProfileBuilder().Build();

        foreach (var state in new[] { AiState.Wait, AiState.Patrol, AiState.SelectAction, AiState.BattleMove, AiState.Gaze, AiState.Move, AiState.Return })
        {
            Assert.Contains(state, profile.States);
        }

        Assert.DoesNotContain(AiState.Attack, profile.States);
        Assert.DoesNotContain(AiState.CommonAction1, profile.States);
    }
}
