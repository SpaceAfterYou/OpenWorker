using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record MonsterSpawnBox : BasicEntity
{
    public MonsterSpawnBox(XElement x) : base(x)
    {
        MonsterList = Enumerable
            .Range(1, 10)
            .Select(id => new Monster(
                x.GetInt32($"m_iMonsterID{id}"), 
                x.GetEnum<MonsterSpawnType>($"m_eType{id}"),
                x.GetInt32($"m_iChance{id}")))
            .ToArray();
        CreationPositionType = x.GetEnum<CreationPositionType>("m_eCreationPositionType");
        MoveType = x.GetEnum<MoveType>("m_eMoveType");
        CreationCondition = x.GetEnum<CreationConditionType>("m_eCreationCondition");
        CreationEffectFile = x.GetString("m_CreationEffectFile");
        WaitCreationDelayTime = x.GetSingle("m_fWaitCreationDelayTime");
        WaitCreationSequenceTime = x.GetSingle("m_fWaitCreationSequenceTime");
        WaitCreationMaxWave = x.GetByte("m_iWaitCreationMaxWave");
        MaxEntityCount = x.GetByte("m_iMaxEntityCount");
        WaitCreationSequenceType = x.GetEnum<WaitCreationSequenceType>("m_eWaitCreationSequenceType");
        Waypoint = x.GetInt32("m_iWaypoint");
        AggroGroup = x.GetInt32("m_iAggroGroupID");
        AggroDistance = x.GetInt32("m_iAggroDistance");
        AggroMaxCount = x.GetInt32("m_iAggroMaxCount");
        LookRatio = x.GetSingle("m_fLookRatio");
        ShowSight = x.GetBool("m_bShowSight");
        ObjectKey = x.GetString("m_szObjectKey");
        ScriptType = x.GetEnum<ScriptType>("m_eScriptType");
        CheckScriptHps = Enumerable.Range(1, 5).Select(id => x.GetByte($"m_iCheckScriptHP{id}")).ToArray();
        Sector = x.GetInt16("m_iSectorID");
        ChangeSpawnAction = x.GetEnum<ChangeSpawnActionType>("m_ChangeSpawnAction");
        ProtectionTarget = x.GetInt32("m_ProtectionTarget");
        RespawnTime = x.GetSingle("m_RespawnTime");
        Step = x.GetByte("m_iStep");
        RespawnType = x.GetEnum<RespawnType>("m_eRespawnType");
        RespawnCondition = x.GetInt32("m_iRespawnCondition");
        Group = x.GetInt32("m_iGroupID");
    }

    /// <summary>
    /// A box of no map that brings one monster in at once at the place given, as a game master calls one in.
    /// </summary>
    public static MonsterSpawnBox At(int monster, Vector3 place, float rotation, int sector)
    {
        var x = new XElement("VMonsterSpawnBox");

        void Add(string name, string value) => x.Add(new XElement(name, new XAttribute("value", value)));
        string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        string Point(Vector3 v) => $"{Number(v.X)},{Number(v.Y)},{Number(v.Z)}";

        Add("m_ID", "0");
        Add("m_iGenerateID", "0");
        Add("iLayerBitmask", nameof(LayerBitmask.Common));
        Add("m_vPosTopLeft", Point(place));
        Add("m_vPosBottomRight", Point(place));
        Add("m_fRotation", Number(rotation));
        Add("m_vSize", Point(Vector3.Zero));
        Add("m_bShowCustomEntity", "FALSE");
        Add("m_ColorLDR", "0,0,0,255");

        for (var slot = 1; slot <= 10; slot++)
        {
            var first = slot == 1;

            Add($"m_iMonsterID{slot}", (first ? monster : 0).ToString(CultureInfo.InvariantCulture));
            Add($"m_eType{slot}", nameof(MonsterSpawnType.Monster));
            Add($"m_iChance{slot}", first ? "10000" : "0");
        }

        Add("m_eCreationPositionType", nameof(CreationPositionType.Center));
        Add("m_eMoveType", nameof(MoveType.Free));
        Add("m_eCreationCondition", nameof(CreationConditionType.Immediate));
        Add("m_CreationEffectFile", string.Empty);
        Add("m_fWaitCreationDelayTime", "0");
        Add("m_fWaitCreationSequenceTime", "0");
        Add("m_iWaitCreationMaxWave", "1");
        Add("m_iMaxEntityCount", "1");
        Add("m_eWaitCreationSequenceType", nameof(WaitCreationSequenceType.All));
        Add("m_iWaypoint", "0");
        Add("m_iAggroGroupID", "0");
        Add("m_iAggroDistance", "0");
        Add("m_iAggroMaxCount", "0");
        Add("m_fLookRatio", "1");
        Add("m_bShowSight", "FALSE");
        Add("m_szObjectKey", string.Empty);
        Add("m_eScriptType", nameof(ScriptType.None));

        for (var check = 1; check <= 5; check++)
        {
            Add($"m_iCheckScriptHP{check}", "0");
        }

        Add("m_iSectorID", sector.ToString(CultureInfo.InvariantCulture));
        Add("m_ChangeSpawnAction", string.Empty);
        Add("m_ProtectionTarget", "0");
        Add("m_RespawnTime", "0");
        Add("m_iStep", "1");
        Add("m_eRespawnType", nameof(RespawnType.None));
        Add("m_iRespawnCondition", "0");
        Add("m_iGroupID", "0");

        return new MonsterSpawnBox(x);
    }

    /// <summary>
    /// </summary>
    public IReadOnlyList<Monster> MonsterList { get; }

    /// <summary>
    ///     Creation Position Type.
    /// </summary>
    public CreationPositionType CreationPositionType { get; }

    /// <summary>
    ///     Move Type.
    /// </summary>
    public MoveType MoveType { get; }

    /// <summary>
    ///     Condition of Creation.
    /// </summary>
    public CreationConditionType CreationCondition { get; }

    /// <summary>
    ///     EffectFile.
    /// </summary>
    public string CreationEffectFile { get; }

    /// <summary>
    ///     If Condition of Creation is 'WaitSignal', Determine whether the signal being generated in a few seconds.
    /// </summary>
    public float WaitCreationDelayTime { get; }

    /// <summary>
    ///     If Condition of Creation is 'WaitSignal', Determines whether every few seconds sequentially generated.
    /// </summary>
    public float WaitCreationSequenceTime { get; }

    /// <summary>
    ///     If Condition of Creation is 'WaitSignal', Not waiting for the signal to determine the maximum of times.
    /// </summary>
    public byte WaitCreationMaxWave { get; }

    /// <summary>
    ///     The maximum number of objects that can be created. If Condition of Creation is 'WaitSignal', And let not exceed
    ///     more than the maximum number of objects when creating objects.
    /// </summary>
    public byte MaxEntityCount { get; }

    /// <summary>
    ///     Whether the spins in order from 1 to 3 monsters that spawn at a time set when you create a sequence generated.
    /// </summary>
    public WaitCreationSequenceType WaitCreationSequenceType { get; }

    /// <summary>
    ///     Waypoint ID.
    /// </summary>
    public int Waypoint { get; }

    /// <summary>
    ///     AggroGroupID.
    /// </summary>
    public int AggroGroup { get; }

    /// <summary>
    ///     AggroDistance.
    /// </summary>
    public int AggroDistance { get; }

    /// <summary>
    ///     AggroMaxCount.
    /// </summary>
    public int AggroMaxCount { get; }

    /// <summary>
    ///     Multiplying the value of Sight.
    /// </summary>
    public float LookRatio { get; }

    /// <summary>
    ///     View the sight will be displayed?
    /// </summary>
    public bool ShowSight { get; }

    /// <summary>
    ///     ObjectKey
    /// </summary>
    public string ObjectKey { get; }

    /// <summary>
    ///     Scene script call type.
    /// </summary>
    public ScriptType ScriptType { get; }

    /// <summary>
    ///     if Scene script call type is HP, the event box being run. HP(100 = 100%).
    /// </summary>
    public IReadOnlyList<byte> CheckScriptHps { get; }

    /// <summary>
    ///     ID of the associated sectors(auto editable)
    /// </summary>
    public int Sector { get; }

    /// <summary>
    ///     Enter the name you used when creating the spawn action.
    /// </summary>
    public ChangeSpawnActionType ChangeSpawnAction { get; }

    /// <summary>
    ///     Specifies the ID of the destination specified by the box spawn protected.
    /// </summary>
    public int ProtectionTarget { get; }

    /// <summary>
    ///     Unit [ms] (Specifies whether to create a monster, who died after a few seconds. Value of 0 is not used for the
    ///     responder.)
    /// </summary>
    public float RespawnTime { get; }

    /// <summary>
    ///     Step index of monster spawn.
    /// </summary>
    public byte Step { get; }

    /// <summary>
    ///     Monster Respawn Type.
    /// </summary>
    public RespawnType RespawnType { get; }

    /// <summary>
    ///     Respawn Condition ID.
    /// </summary>
    public int RespawnCondition { get; }

    /// <summary>
    ///     GroupID
    /// </summary>
    public int Group { get; }
}