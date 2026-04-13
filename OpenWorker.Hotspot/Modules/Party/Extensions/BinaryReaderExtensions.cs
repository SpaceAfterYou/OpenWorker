using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Hotspot.Modules.Party.Extensions;

public static class BinaryReaderExtensions
{
    extension(BinaryReader reader)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyType ReadPartyType() => (PartyType)reader.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyUpdateType ReadPartyUpdateType() => (PartyUpdateType)reader.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyMatchingExit ReadPartyAutoMatchingExit() => (PartyMatchingExit)reader.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyReasonModeMazeMatchingExit ReadReasonModeMazeMatchinExit() => (PartyReasonModeMazeMatchingExit)reader.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyApplyError ReadPartyApplyError() => (PartyApplyError)reader.ReadInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyMemberInfoValue[] ReadPartyMemberInfoList_Int() => Enumerable
            .Range(0, reader.ReadInt32())
            .Select(_ => new PartyMemberInfoValue(reader))
            .ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyMemberInfoValue[] ReadPartyMemberInfoList_Byte() => Enumerable
            .Range(0, reader.ReadByte())
            .Select(_ => new PartyMemberInfoValue(reader))
            .ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyApplyMemberValue[] ReadPartyApplyMemberList() => Enumerable
            .Range(0, PartyModuleDefines.RecruitAppliesCount)
            .Select(_ => new PartyApplyMemberValue(reader))
            .ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public PartyMemberInfoValue[] ReadPartyMemberInfoList_Fixed() => Enumerable
            .Range(0, PartyModuleDefines.MaxMemberCount)
            .Select(_ => new PartyMemberInfoValue(reader))
            .ToArray();
    }
}
