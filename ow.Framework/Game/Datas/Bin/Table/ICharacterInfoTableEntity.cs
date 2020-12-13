using System;
using System.Collections.Generic;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = UInt16;

    public interface ICharacterInfoTableEntity : ITableEntity<KeyType>
    {
        ushort Unknown6 { get; }
        byte Unknown7 { get; }
        string Unknown8 { get; }
        string Unknown9 { get; }
        string DialogueTalkImgPath { get; }
        string Token { get; }
        string Unknown12 { get; }
        string CutScenePath { get; }
        string GhostTalkImgPath { get; }
        IReadOnlyList<uint> Unknown13 { get; }
        public ushort Unknown24 { get; }
        public ushort Unknown25 { get; }
        uint DefaultWeapon { get; }
        IReadOnlyList<uint> DefaultOutfits { get; }
        uint Unknown31 { get; }
        short Unknown32 { get; }
        short Unknown33 { get; }
        short Unknown34 { get; }
        short Unknown35 { get; }
        short Unknown36 { get; }
        byte Unknown37 { get; }
        uint Unknown38 { get; }
        uint Unknown39 { get; }
        uint Unknown40 { get; }
        ushort Unknown41 { get; }
        float Unknown42 { get; }
        short Unknown43 { get; }
        ushort Unknown44 { get; }
        ushort Unknown45 { get; }
        ushort Unknown46 { get; }
        ushort Unknown47 { get; }
        ushort Unknown48 { get; }
        ushort Unknown49 { get; }
        float Unknown50 { get; }
        byte Unknown51 { get; }
        ushort Unknown52 { get; }
        ushort Unknown53 { get; }
        ushort Unknown54 { get; }
        ushort Unknown55 { get; }
        ushort Unknown56 { get; }
        ushort Unknown57 { get; }
        ushort Unknown58 { get; }
        ushort Unknown59 { get; }
        ushort Unknown60 { get; }
        short Unknown61 { get; }
        ushort Unknown62 { get; }
        ushort Unknown63 { get; }
        ushort Unknown64 { get; }
        IReadOnlyList<uint> DefaultPassiveSkill { get; }
        IReadOnlyList<uint> DefaultActiveSkill { get; }
        IReadOnlyList<uint> DefaultGenericSkill { get; }
        uint Unknown77 { get; }
        uint Unknown78 { get; }
        uint Unknown79 { get; }
        uint Unknown80 { get; }
        uint Unknown81 { get; }
        uint Unknown82 { get; }
        uint Unknown83 { get; }
        uint Unknown84 { get; }
        ushort Unknown85 { get; }
        float Unknown86 { get; }
        uint Unknown87 { get; }
        uint Unknown88 { get; }
        ushort Unknown89 { get; }
        byte Unknown90 { get; }
    }
}