using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Skill.Requests;

namespace OpenWorker.Hotspot.Modules.Skill.Extensions;

public static class BinaryReaderExtensions
{
    extension(BinaryReader reader)
    {
        public SkillDmgValue[] ReadSkillDmgValueList() => Enumerable
            .Range(0, reader.ReadByte())
            .Select(e => new SkillDmgValue(reader))
            .ToArray();

        public ushort[] ReadSkillDeck() => Enumerable
            .Range(0, SkillModuleDefines.BonusSkillDeckCount)
            .Select(e => reader.ReadUInt16())
            .ToArray();

        public ActorValue[] ReadPreTargetList() => Enumerable
            .Range(0, reader.ReadByte())
            .Select(e => new ActorValue(reader))
            .ToArray();

        public SkillDeckValueList[] ReadSkillDeckValueList() => Enumerable
            .Range(0, Math.Min(reader.ReadByte(), SkillModuleDefines.SkillDeckCount))
            .Select(e => new SkillDeckValueList(reader))
            .ToArray();
    }
}
