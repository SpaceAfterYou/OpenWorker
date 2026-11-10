using System.Numerics;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The mind of a helper: it follows its owner, is put next to the owner when it is too far, and gets up again when it is
/// beaten down.
/// </summary>
[TestClass]
public sealed class HelperAiTest
{
    private const float Step = 0.1f;
    private const int RecoverySkill = 50000;

    /// <summary>
    /// A helper that walks in a straight line, with an owner who stands where the test puts it.
    /// </summary>
    private sealed class Helper : IAiHost
    {
        public Vector3 Position { get; set; }
        public Vector3 HomePosition => OwnerPosition;
        public float Yaw { get; set; }
        public float Radius => 40f;
        public int Health { get; set; } = 1000;
        public int MaxHealth => 1000;
        public bool IsDead => false;
        public bool CanMove => !IsAttacking;
        public bool CanAttack => !IsAttacking;
        public bool IsMoving => Destination is not null;
        public bool IsRooted => false;
        public bool IsAttacking { get; set; }
        public bool IsBattlePose { get; set; }
        public float Age { get; set; }
        public Vector3? Destination { get; set; }
        public Vector3 OwnerPosition { get; set; }
        public List<int> Used { get; } = [];
        public List<Vector3> Warps { get; } = [];
        public bool Invincible { get; private set; }

        public AiTarget? Owner => new AiTarget(1, OwnerPosition, 35f, 0f, 500, 1000, false);

        public AiSkill? GetSkill(int slot) => null;
        public AiTarget? GetTarget(int id) => null;
        public AiTarget? FindEnemy(float distance) => null;
        public int CountEnemies(float distance) => 0;
        public int CountSectorMonsters() => 0;
        public int CountSpawnedMonsters() => 0;
        public void StopMoving(bool send) => Destination = null;

        public bool MoveTo(Vector3 destination, bool run)
        {
            Destination = destination;

            return true;
        }

        public void FaceTo(Vector3 point) => Yaw = AiMath.YawTo(Position, point);
        public void SetBattlePose(bool battle, bool playMotion) => IsBattlePose = battle;
        public void SetTarget(int id) { }
        public void SetGaze(bool gazing) { }

        public bool UseSkill(AiSkill skill, AiTarget target)
        {
            Used.Add(skill.Id);

            return true;
        }

        public float PlayAction(string name) => 0f;
        public void SetInvincible(bool invincible) => Invincible = invincible;

        public int BuffsCleared { get; private set; }

        public void ClearBuffs() => BuffsCleared++;
        public void RestoreHealth() => Health = MaxHealth;
        public void Teleport(Vector3 position) => Position = position;
        public void Die() { }

        public void WarpTo(Vector3 position)
        {
            Warps.Add(position);
            Position = position;
            Destination = null;
        }

        public void Heal(int amount) => Health = Math.Min(MaxHealth, Health + amount);

        public AiSkill? FindSkill(int id) => new(id, 0, 0, 0, 0, 0, 0, true, false);

        public void Advance(float seconds)
        {
            Age += seconds;

            if (Destination is { } destination)
            {
                var offset = destination - Position;
                var walk = 600f * seconds;

                if (offset.Length() <= walk)
                {
                    Position = destination;
                    Destination = null;
                }
                else
                {
                    Position += Vector3.Normalize(offset) * walk;
                }
            }
        }
    }

    /// <summary>
    /// What the scripts of the helpers say about following: wait until the owner is more than 320 away, then go to a place
    /// 100 to 250 from the owner; too far (3500 out of a fight, 9000 in one), be put next to the owner. Beaten down, get up
    /// with skill 50000.
    /// </summary>
    private static AiProfile Profile()
    {
        var builder = new AiProfileBuilder();

        builder.SetAiCheckTime(Step);
        builder.SetHelperFarDist(3500, 9000);
        builder.SetRecoverySkill(RecoverySkill);
        builder.RegisterConditions(AiState.Wait, AiState.Move, AiVariable.GuardDistance, ">", 320);
        builder.RegisterStateVars(AiState.Move, AiStateVariable.TargetDistance, 100, 250);
        builder.RegisterStateVars(AiState.Move, AiStateVariable.Angle, 0, 40);

        return builder.Build();
    }

    private static void Run(AiMachine machine, Helper helper, float seconds)
    {
        for (var time = 0f; time < seconds; time += Step / 2)
        {
            helper.Advance(Step / 2);
            machine.Update(Step / 2);
        }
    }

    /// <summary>
    /// The guard distance of the script reads the owner: a helper whose owner walks away goes after it and stops near it,
    /// and one whose owner is farther than the script allows is put next to the owner at once (27/13 tells the clients).
    /// </summary>
    [TestMethod]
    public void AHelperFollowsItsOwnerAndIsPutNextToItWhenTooFar()
    {
        var helper = new Helper { Position = Vector3.Zero, OwnerPosition = Vector3.Zero };
        var machine = new AiMachine(Profile(), helper, new Random(1));

        machine.Start();
        Run(machine, helper, 1f);

        Assert.AreEqual(AiState.Wait, machine.State, "Next to its owner the helper waits.");

        helper.OwnerPosition = new Vector3(1500, 0, 0);
        Run(machine, helper, 4f);

        var near = Vector3.Distance(helper.Position, helper.OwnerPosition);

        Assert.IsTrue(near is >= 90 and <= 260, $"The helper went after its owner and stopped near it ({near:F0} away).");
        Assert.IsEmpty(helper.Warps, "Walking was enough.");

        helper.OwnerPosition = new Vector3(1500, 5000, 0);
        Run(machine, helper, 0.5f);

        Assert.HasCount(1, helper.Warps, "Too far from its owner, the helper is put next to it.");
        Assert.AreEqual(helper.OwnerPosition, helper.Warps[0]);
    }

    /// <summary>
    /// A helper beaten down does not die: it gets up with its recovery skill, cannot be hurt, heals a twentieth of its life
    /// every second until it is whole, and then fights again. Whatever buffs and debuffs it had are taken away from the moment
    /// it starts to get up and at every step of the recovery.
    /// </summary>
    [TestMethod]
    public void AHelperBeatenDownGetsUpInsteadOfDying()
    {
        var helper = new Helper { Health = 1 };
        var machine = new AiMachine(Profile(), helper, new Random(1));

        machine.Start();
        machine.Recover();

        Assert.AreEqual(AiState.Recovery, machine.State);
        Assert.IsTrue(helper.Invincible, "Nothing hurts a helper that is getting up.");
        Assert.Contains(RecoverySkill, helper.Used);
        Assert.AreEqual(1, helper.BuffsCleared, "Its buffs go when it starts to get up.");

        Run(machine, helper, 1.2f);

        Assert.IsGreaterThan(1, helper.BuffsCleared, "And again at every step of the recovery.");

        Assert.AreEqual(1 + 50, helper.Health, "A twentieth of its life after a second.");

        Run(machine, helper, 20f);

        Assert.AreEqual(helper.MaxHealth, helper.Health);
        Assert.AreNotEqual(AiState.Recovery, machine.State, "Whole again, it fights on.");
        Assert.IsFalse(helper.Invincible);
    }
}
