using ow.Framework.Game.Datas.World.Table.Types;
using ow.Framework.IO.File.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ow.Framework.Game.Datas.World.Table.EventBox
{
    public sealed record VMonsterSpawnBox : VEntity
    {
        /// <summary>
        ///
        /// </summary>
        public IReadOnlyList<VMonster> Monsters { get; }

        /// <summary>
        /// Creation Position Type.
        /// </summary>
        public CreationPositionType CreationPositionType { get; }

        /// <summary>
        /// Move Type.
        /// </summary>
        public MoveType MoveType { get; }

        /// <summary>
        /// Condition of Creation.
        /// </summary>
        public CreationConditionType CreationCondition { get; }

        /// <summary>
        /// EffectFile.
        /// </summary>
        public string CreationEffectFile { get; }

        /// <summary>
        /// If Condition of Creation is 'WaitSignal', Determine whether the signal being generated in a few seconds.
        /// </summary>
        public float WaitCreationDelayTime { get; }

        /// <summary>
        /// If Condition of Creation is 'WaitSignal', Determines whether every few seconds sequentially generated.
        /// </summary>
        public float WaitCreationSequenceTime { get; }

        /// <summary>
        /// If Condition of Creation is 'WaitSignal', Not waiting for the signal to determine the maximum of times.
        /// </summary>
        public byte WaitCreationMaxWave { get; }

        /// <summary>
        /// The maximum number of objects that can be created. If Condition of Creation is 'WaitSignal', And let not exceed more than the maximum number of objects when creating objects.
        /// </summary>
        public byte MaxEntityCount { get; }

        /// <summary>
        /// Whether the spins in order from 1 to 3 monsters that spawn at a time set when you create a sequence generated.
        /// </summary>
        public WaitCreationSequenceType WaitCreationSequenceType { get; }

        /// <summary>
        /// Waypoint ID.
        /// </summary>
        public uint Waypoint { get; }

        /// <summary>
        /// AggroGroupID.
        /// </summary>
        public ushort AggroGroup { get; }

        /// <summary>
        /// AggroDistance.
        /// </summary>
        public uint AggroDistance { get; }

        /// <summary>
        /// AggroMaxCount.
        /// </summary>
        public ushort AggroMaxCount { get; }

        /// <summary>
        /// Multiplying the value of Sight.
        /// </summary>
        public float LookRatio { get; }

        /// <summary>
        /// View the sight will be displayed?
        /// </summary>
        public bool ShowSight { get; }

        /// <summary>
        /// ObjectKey
        /// </summary>
        public string ObjectKey { get; }

        /// <summary>
        /// Scene script call type.
        /// </summary>
        public ScriptType ScriptType { get; }

        /// <summary>
        /// if Scene script call type is HP, the event box being run. HP(100 = 100%).
        /// </summary>
        public IReadOnlyList<byte> CheckScriptHps { get; }

        /// <summary>
        /// ID of the associated sectors(auto editable)
        /// </summary>
        public ushort Sector { get; }

        /// <summary>
        /// Enter the name you used when creating the spawn action.
        /// </summary>
        public string ChangeSpawnAction { get; }

        /// <summary>
        /// Specifies the ID of the destination specified by the box spawn protected.
        /// </summary>
        public ushort ProtectionTarget { get; }

        /// <summary>
        /// Unit [ms] (Specifies whether to create a monster, who died after a few seconds. Value of 0 is not used for the responder.)
        /// </summary>
        public float RespawnTime { get; }

        /// <summary>
        /// Step index of monster spawn.
        /// </summary>
        public byte Step { get; }

        /// <summary>
        /// Monster Respawn Type.
        /// </summary>
        public RespawnType RespawnType { get; }

        /// <summary>
        /// Respawn Condition ID.
        /// </summary>
        public uint RespawnCondition { get; }

        /// <summary>
        /// GroupID
        /// </summary>
        public uint GroupId { get; }

        internal VMonsterSpawnBox(XmlNode xml) : base(xml)
        {
            Monsters = Enumerable
                .Range(1, 10)
                .Select(id => new VMonster(xml.GetUInt32($"m_iMonsterID{id}"), xml.GetEnum<MonsterSpawnType>($"m_eType{id}"), xml.GetUInt32($"m_iChance{id}")))
                .ToArray();
            CreationPositionType = xml.GetEnum<CreationPositionType>("m_eCreationPositionType");
            MoveType = xml.GetEnum<MoveType>("m_eMoveType");
            CreationCondition = xml.GetEnum<CreationConditionType>("m_eCreationCondition");
            CreationEffectFile = xml.GetString("m_CreationEffectFile");
            WaitCreationDelayTime = xml.GetSingle("m_fWaitCreationDelayTime");
            WaitCreationSequenceTime = xml.GetSingle("m_fWaitCreationSequenceTime");
            WaitCreationMaxWave = xml.GetByte("m_iWaitCreationMaxWave");
            MaxEntityCount = xml.GetByte("m_iMaxEntityCount");
            WaitCreationSequenceType = xml.GetEnum<WaitCreationSequenceType>("m_eWaitCreationSequenceType");
            Waypoint = xml.GetUInt32("m_iWaypoint");
            AggroGroup = xml.GetUInt16("m_iAggroGroupID");
            AggroDistance = xml.GetUInt32("m_iAggroDistance");
            AggroMaxCount = xml.GetUInt16("m_iAggroMaxCount");
            LookRatio = xml.GetSingle("m_fLookRatio");
            ShowSight = xml.GetBool("m_bShowSight");
            ObjectKey = xml.GetString("m_szObjectKey");
            ScriptType = xml.GetEnum<ScriptType>("m_eScriptType");
            CheckScriptHps = Enumerable.Range(1, 5).Select(id => xml.GetByte($"m_iCheckScriptHP{id}")).ToArray();
            Sector = xml.GetUInt16("m_iSectorID");
            ChangeSpawnAction = xml.GetString("m_ChangeSpawnAction");
            ProtectionTarget = xml.GetUInt16("m_ProtectionTarget");
            RespawnTime = xml.GetSingle("m_RespawnTime");
            Step = xml.GetByte("m_iStep");
            RespawnType = xml.GetEnum<RespawnType>("m_eRespawnType");
            RespawnCondition = xml.GetUInt32("m_iRespawnCondition");
            GroupId = xml.GetUInt32("m_iGroupID");
        }
    }
}
