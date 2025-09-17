using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Extensions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command)]
public readonly struct WorldInInfoMonsterResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.InInfoMonster;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
    }
}

public readonly struct STNpcInfo
{
    public required ActorValue uxActorID { get; init; }
    public required WorldValue stPosInfo { get; init; }
    public required int nHP { get; init; }
    public required int nWayPointID { get; init; }
    public required int nSectorID { get; init; }
    public required byte byLevel { get; init; }
    public required int nTableID { get; init; }
}

public readonly struct StatKeyValuePair
{
    public required STAT_TYPE byIndex { get; init; }
    public required float statValue { get; init; }
}

public readonly struct STMonsterInfo
{
    public required STNpcInfo stNpcInfo { get; init; }
    public ActorValue uxParentActorID { get; init; }
    public required int nSpawnBoxID { get; init; }
    public int nMotionClass { get; init; }
    public required bool bBattlePos { get; init; }
    public required float fCurSuperArmor { get; init; }
    public required float fMaxSuperArmor { get; init; }
    public required StatKeyValuePair[] vecStat { get; init; }
}

public readonly struct CreatureComponent : IEquatable<CreatureComponent>
{
    public int Identifier { get; init; }
    public byte Level { get; init; }

    public static implicit operator int(CreatureComponent obj)
    {
        return obj.Identifier;
    }

    public bool Equals(CreatureComponent other)
    {
        return Identifier == other.Identifier;
    }

    public override bool Equals(object? obj)
    {
        return obj is CreatureComponent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Identifier;
    }
    
    public static bool operator ==(CreatureComponent left, CreatureComponent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CreatureComponent left, CreatureComponent right)
    {
        return !(left == right);
    }
}
public readonly record struct NonPlayableCreatureComponent
{
    public int Waypoint { get; init; }
    public int Sector { get; init; }
    public int SpawnBox { get; init; }
}

[HotspotMessage(Group, Command)]
public readonly struct WorldOtherInfosMonsterResponse(Arch.Core.World world, params Entity[] list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.OtherInfosMonster;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write((short)list.Length);
        
        foreach (var item in list)
        {
            var creature = world.Get<CreatureComponent>(item);
            var nonPlayableCreature = world.Get<NonPlayableCreatureComponent>(item);
            
            writer.WriteMonster(new STMonsterInfo
            {
                stNpcInfo = new STNpcInfo
                {
                    uxActorID = world.Get<ActorComponent>(item),
                    stPosInfo = world.Get<WorldComponent>(item),
                    nHP = 250,
                    nWayPointID = nonPlayableCreature.Waypoint,
                    nSectorID = nonPlayableCreature.Sector,
                    byLevel = creature.Level,
                    nTableID = creature.Identifier
                },
                // uxParentActorID = World.Get<ActorComponent>(player),
                nSpawnBoxID = nonPlayableCreature.SpawnBox,
                // nMotionClass = 0,
                bBattlePos = true,
                fCurSuperArmor = 100.0f,
                fMaxSuperArmor = 100.0f,
                vecStat = []
            });
        }
    }
}

[HotspotMessage(Group, Command)]
public readonly struct WorldEnterMazeLimitCountUpdateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.EnterMazeLimitCountUpdate;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
    }
}
