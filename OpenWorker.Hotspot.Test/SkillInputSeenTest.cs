using System.Numerics;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Server.Common.Services;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the others see of the input of a player's skill: the key it holds, the target it picks, the sub animation, the
/// charging, the targets it aims at and the places of the trap it drags.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SkillInputSeenTest
{
    private static readonly SkillOpcode[] Seen =
    [
        SkillOpcode.SkillKeypressBt,
        SkillOpcode.SkillTargetChangeBt,
        SkillOpcode.SkillSubInputBt,
        SkillOpcode.SkillChargingStartBt,
        SkillOpcode.SkillChargingEndBt,
        SkillOpcode.PreTargetList,
        SkillOpcode.TrapPosUpdateBt
    ];

    /// <summary>
    /// Every input goes to the players who see the player and not to the player, with the actor of the player first, as the
    /// original sends it; nobody out of sight hears it.
    /// </summary>
    [TestMethod]
    public async Task TheInputOfASkillGoesToTheOthersWhoSeeThePlayerAndNotToThePlayer()
    {
        using var district = new DistrictHarness();

        var caster = district.Join(100f, 100f);
        var near = district.Join(200f, 100f);
        var far = district.Join(WorldOptions.DefaultVisibilityRadius * 4f, 100f);

        var relay = new SkillInputRelay(district.Ecs, new ChannelAudience(district.Ecs, district.Channels));

        await district.ResetAsync(caster, near, far);

        relay.KeyPressed(caster.Entity, true);
        relay.TargetChanged(caster.Entity, new ActorValue(3, ActorType.Monster));
        relay.SubInput(caster.Entity, 2);
        relay.Charging(caster.Entity, released: false, pressTime: 0f);
        relay.Charging(caster.Entity, released: true, pressTime: 1.5f);
        relay.PreTargets(caster.Entity, [new ActorValue(3, ActorType.Monster)]);
        relay.TrapMoved(caster.Entity, 24000111, 68, new Vector3(4f, 5f, 6f));

        await district.SettleAsync(caster, near, far);

        CollectionAssert.AreEqual(
            Seen.Select(e => (byte)e).ToArray(),
            near.Received().Where(e => e.Opcode.Group == (byte)GroupOpcode.Skill).Select(e => e.Opcode.Command).ToArray(),
            "The player next to it sees every input, in the order it was given.");

        Assert.IsTrue(near.Received().Where(e => e.Opcode.Group == (byte)GroupOpcode.Skill).All(e => e.Actor == caster.Actor), "The actor of the player comes first.");
        Assert.IsFalse(caster.Received().Any(e => e.Opcode.Group == (byte)GroupOpcode.Skill), "The player is told nothing of its own input.");
        Assert.IsFalse(far.Received().Any(e => e.Opcode.Group == (byte)GroupOpcode.Skill), "Nobody out of sight sees it.");
    }

    /// <summary>
    /// What the client says is sent on only when it is a number a client sends: a sub animation out of range, a time that is
    /// not a time, a crowd of targets and a place that is not a place are dropped.
    /// </summary>
    [TestMethod]
    public async Task ANumberOutOfBoundsIsNotSentOn()
    {
        using var district = new DistrictHarness();

        var caster = district.Join(100f, 100f);
        var near = district.Join(200f, 100f);

        var relay = new SkillInputRelay(district.Ecs, new ChannelAudience(district.Ecs, district.Channels));

        await district.ResetAsync(caster, near);

        relay.SubInput(caster.Entity, -1);
        relay.SubInput(caster.Entity, SkillInputRelay.MaxSubAnimation + 1);
        relay.Charging(caster.Entity, released: true, pressTime: float.NaN);
        relay.Charging(caster.Entity, released: true, pressTime: -1f);
        relay.Charging(caster.Entity, released: true, pressTime: SkillInputRelay.MaxPressTime + 1f);
        relay.PreTargets(caster.Entity, Enumerable.Repeat(new ActorValue(3, ActorType.Monster), SkillInputRelay.MaxPreTargets + 1).ToArray());
        relay.TrapMoved(caster.Entity, 24000111, 68, new Vector3(float.NaN, 0f, 0f));

        await district.SettleAsync(caster, near);

        Assert.IsFalse(near.Received().Any(e => e.Opcode.Group == (byte)GroupOpcode.Skill), "Nothing is sent on.");
    }
}
