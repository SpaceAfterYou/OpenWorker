using System.Diagnostics;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Skill.Requests;

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

        public void WriteSkillDeck(ushort[] values)
        {
            Debug.Assert(values.Length == SkillModuleDefines.BonusSkillDeckCount);

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

        public void Write(SkillDeckValueList[] values)
        {
            Debug.Assert(values.Length == SkillModuleDefines.BonusSkillDeckCount);

            writer.Write((byte)values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }
    }
}
