using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Skill.Enums;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Extensions;

public static class BinaryWriterExtensions
{
    extension(BinaryWriter writer)
    {
        public void Write(SkillDmgValue[] values)
        {
            writer.Write((byte)values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }

        public void WriteSkillDeckBonus(ushort[] values)
        {
            Debug.Assert(values.Length == SkillModuleDefines.SkillDeckBonusCount);

            foreach (var value in values)
            {
                writer.Write(value);
            }
        }

        public void WritePreTargetList(ActorValue[] values)
        {
            writer.Write((byte)values.Length);

            foreach (var value in values)
            {
                writer.Write(value);
            }
        }

        public void Write(SkillInfoValue[] values)
        {
            writer.Write((byte)values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }

        public void Write(SkillDeckValue[] values)
        {
            Debug.Assert(values.Length < SkillModuleDefines.SkillDeckCount);

            writer.Write((byte)values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Write(SkillType value) => writer.Write((byte)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Write(CreatureDefenseType value) => writer.Write((byte)value);
    }
}
