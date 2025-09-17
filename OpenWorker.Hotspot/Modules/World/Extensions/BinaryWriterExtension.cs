using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Enums;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.Hotspot.Modules.World.Extensions;

internal static class BinaryWriterExtension
{
    internal static void Write(this BinaryWriter writer, WorldWrapResult value)
    {
        writer.Write((byte)value);
    }
    
    internal static void Write(this BinaryWriter writer, WarpValue value)
    {
        writer.Write(value.HasError);
        writer.Write(value.Position);
        writer.Write(value.Rotation);
    }

    internal static void WriteNpc(this BinaryWriter writer, STNpcInfo value)
    {
        writer.WriteActor(value.uxActorID);
        
        writer.Write(value.stPosInfo.Position);
        writer.Write(value.stPosInfo.Rotation);
        
        writer.Write(value.nHP);
        writer.Write(value.nWayPointID);
        writer.Write(value.nSectorID);
        writer.Write(value.byLevel);
        writer.Write(value.nTableID);
    }

    internal static void Write(this BinaryWriter writer, STAT_TYPE value)
    {
        writer.Write((byte)value);
    }
    
    internal static void Write(this BinaryWriter writer, StatKeyValuePair value)
    {
        writer.Write(value.byIndex);
        writer.Write(value.statValue);
    }
    
    internal static void Write(this BinaryWriter writer, StatKeyValuePair[] value)
    {
        writer.Write((byte)value.Length);
        
        foreach (var item in value)
        {
            writer.Write(item);
        }
    }

    internal static void WriteMonster(this BinaryWriter writer, STMonsterInfo value)
    {
        writer.WriteNpc(value.stNpcInfo);
        writer.WriteActor(value.uxParentActorID);
        writer.Write(value.nSpawnBoxID);
        writer.Write(value.nMotionClass);
        writer.Write(value.bBattlePos);
        writer.Write(value.fCurSuperArmor);
        writer.Write(value.fMaxSuperArmor);
        writer.Write(value.vecStat);
    }
}