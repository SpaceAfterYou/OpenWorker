using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Server;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Server.Common;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the others see of a player: the skill it starts (06/10).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayerSkillSeenTest
{
    private const int Strike = 12001211;
    private const int Unlearned = 12000411;

    private static readonly ReadOnlyCollection<SkillRow> Skills = new(
    [
        TestRows.Build<SkillRow>(
            ("Id", Strike), ("Group", Strike), ("Level", 1), ("UsePosition", 1), ("UseClass", 0), ("RequiredLevel", 1),
            ("SkillType", 2), ("CostAttribute", 2), ("Cost", 35), ("Cooldown", 5000), ("CooldownGroup", Strike), ("GlobalCooldown", 500)),
        TestRows.Build<SkillRow>(
            ("Id", Unlearned), ("Group", Unlearned), ("Level", 1), ("UsePosition", 1), ("UseClass", 0), ("RequiredLevel", 1),
            ("SkillType", 2), ("CostAttribute", 2), ("Cost", 35), ("Cooldown", 5000), ("CooldownGroup", Unlearned), ("GlobalCooldown", 500))
    ]);

    private static CombatTables EmptyTables() =>
        new(new ReadOnlyCollection<MonsterRow>([]), new ReadOnlyCollection<StatusRow>([]), Skills, new ReadOnlyCollection<CharacterInfoRow>([]));

    /// <summary>
    /// The skill is the player's own: the others who see the player play it, the player gets the answer only and a skill it
    /// does not have is played by nobody.
    /// </summary>
    [TestMethod]
    public async Task ASkillAPlayerStartsIsPlayedByTheOthersAndNotByThePlayer()
    {
        using var district = new DistrictHarness();

        var caster = district.Join(100f, 100f);
        var near = district.Join(200f, 100f);
        var far = district.Join(WorldOptions.DefaultVisibilityRadius * 4f, 100f);

        district.Ecs.Set(caster.Entity, new LevelComponent(1, 0));
        district.Ecs.Set(caster.Entity, new AbilityComponent
        {
            Health = new AbilityComponentEntry { Current = 100, Max = 100 },
            SoulGain = new AbilityComponentEntry { Current = 100, Max = 100 },
            SoulVapor = new AbilityComponentEntry { Current = 0, Max = 0 },
            Stamina = new AbilityComponentEntry { Current = 100, Max = 100 },
            SuperArmor = new AbilityComponentEntry { Current = 0, Max = 0 },
            Speed = new SpeedComponentEntry { Attack = 1, Move = 1 }
        });
        district.Ecs.Set(caster.Entity, new SkillLearnedComponent { Values = [new SkillInfoValue { Skill = Strike, Divergence = 7 }] });
        district.Ecs.Set(caster.Entity, new SkillDeckComponent { SlotCount = 3, SlotList = [new SkillDeckValue { Position = 0, Skill1 = Strike }] });

        var service = new SkillService(
            district.Ecs,
            new SkillUse(district.Ecs, Skills, new PlayerGauges(district.Ecs, EmptyTables())),
            new ChannelAudience(district.Ecs, district.Channels));

        await district.ResetAsync(caster, near, far);

        await service.OnHandleAsync(new ServiceHandleContext(caster.Entity, CancellationToken.None), Request(Strike, parent: 12001200, deck: 2, trap: 4242));
        await district.SettleAsync(caster, near, far);

        var seen = near.Received().Single(e => e.Is(GroupOpcode.Skill, (byte)SkillOpcode.ActiveSkillBt));

        Assert.AreEqual(caster.Actor, seen.Actor, "The actor of the player comes first.");
        Assert.AreEqual(Strike, BinaryPrimitives.ReadInt32LittleEndian(seen.Body.AsSpan(4)));
        Assert.AreEqual(7, BinaryPrimitives.ReadInt32LittleEndian(seen.Body.AsSpan(4 + 4 + 19)), "The divergence is the one of the player.");
        Assert.AreEqual(4242, BinaryPrimitives.ReadInt32LittleEndian(seen.Body.AsSpan(seen.Body.Length - 4)));
        CollectionAssert.AreEqual(
            new[] { (byte)SkillOpcode.ActiveSkillRes },
            caster.Received().Where(e => e.Opcode.Group == (byte)GroupOpcode.Skill).Select(e => e.Opcode.Command).ToArray(),
            "The player gets the answer only.");
        Assert.IsFalse(far.Received().Any(e => e.Opcode.Group == (byte)GroupOpcode.Skill), "Nobody out of sight sees it.");

        DistrictHarness.Forget(caster, near);

        await service.OnHandleAsync(new ServiceHandleContext(caster.Entity, CancellationToken.None), Request(Unlearned, parent: 0, deck: 0, trap: 1));
        await district.SettleAsync(caster, near);

        Assert.IsFalse(near.Received().Any(e => e.Opcode.Group == (byte)GroupOpcode.Skill), "A skill the player does not have is played by nobody.");
    }


    private static SkillActiveSkillRequest Request(int skill, int parent, byte deck, int trap)
    {
        Span<byte> bytes = stackalloc byte[4 + 4 + 19 + 4 + 4 + 1 + 4];

        var writer = new PacketWriter(bytes);

        writer.WriteInt32(skill);
        writer.WriteInt32(0);
        writer.WriteSingle(150f);
        writer.WriteSingle(100f);
        writer.WriteSingle(0f);
        writer.WriteSingle(45f);
        writer.WriteInt16(0);
        writer.WriteByte(0);
        writer.WriteInt32(0);
        writer.WriteInt32(parent);
        writer.WriteByte(deck);
        writer.WriteInt32(trap);

        var reader = new PacketReader(bytes);

        return SkillActiveSkillRequest.Read(ref reader);
    }
}
