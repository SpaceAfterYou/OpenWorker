using System.Runtime.CompilerServices;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Skill.Enums;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Extensions;

public static class BinaryReaderExtensions
{
    extension(BinaryReader reader)
    {
        public SkillDmgValue[] ReadSkillDmgValueList() => Enumerable
            .Range(0, reader.ReadByte())
            .Select(e => new SkillDmgValue(reader))
            .ToArray();

        public ushort[] ReadSkillDeckBonus() => Enumerable
            .Range(0, SkillModuleDefines.SkillDeckBonusCount)
            .Select(e => reader.ReadUInt16())
            .ToArray();

        public ActorValue[] ReadPreTargetList() => Enumerable
            .Range(0, reader.ReadByte())
            .Select(e => new ActorValue(reader))
            .ToArray();

        public SkillDeckValue[] ReadSkillDeckValueList() => Enumerable
            .Range(0, Math.Min(reader.ReadByte(), SkillModuleDefines.SkillDeckCount))
            .Select(e => new SkillDeckValue(reader))
            .ToArray();

        public SkillInfoValue[] ReadSkillInfoValueList() => Enumerable
            .Range(0, reader.ReadByte())
            .Select(e => new SkillInfoValue(reader))
            .ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public SkillType ReadSkillType() => (SkillType)reader.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public CreatureDefenseType ReadTypeOfDefense() => (CreatureDefenseType)reader.ReadByte();
    }
}
