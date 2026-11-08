using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Hotspot.Modules.Party.Extensions;

public static class BinaryWriterExtensions
{
    extension(BinaryWriter writer)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Write(PartyType value) => writer.Write((byte)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Write(PartyUpdateType value) => writer.Write((byte)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Write(PartyMatchingExit value) => writer.Write((byte)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Write(PartyReasonModeMazeMatchingExit value) => writer.Write((byte)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Write(PartyApplyError value) => writer.Write((int)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteFixed(PartyMemberInfoValue[] values)
        {
            Debug.Assert(values.Length <= PartyModuleDefines.MaxMemberCount);

            // The client always reads a full party.
            for (var i = 0; i < PartyModuleDefines.MaxMemberCount; i++)
            {
                (i < values.Length ? values[i] : PartyMemberInfoValue.Empty).Write(writer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void WriteFixed(PartyApplyMemberValue[] values)
        {
            Debug.Assert(values.Length == PartyModuleDefines.RecruitAppliesCount);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }

        public void WriteWithByteCount(PartyMemberInfoValue[] values)
        {
            writer.Write((byte)values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }

        public void WriteWithIntCount(PartyMemberInfoValue[] values)
        {
            writer.Write(values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }

        public void Write(PartyAwaiterValue[] values)
        {
            writer.Write(values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }

        public void Write(PartyRecruitMemberValue[] values)
        {
            writer.Write(values.Length);

            foreach (var value in values)
            {
                value.Write(writer);
            }
        }
    }
}
