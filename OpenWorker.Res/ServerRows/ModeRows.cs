using OpenWorker.Extensions;
using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

// The game modes of a maze that its script starts: waves of monsters to survive, a monster to defend, a boss that asks for
// something to be done in time. The numbers are the ones the script names (<c>StartSurvivalMode(10)</c>); the client has none
// of these tables (its own <c>tb_Mode_Operation</c> is another table, with the texts).

/// <summary>
/// A mode in which the monsters of some spawn boxes come in waves, and the maze is won when the last wave is dead.
/// </summary>
[BinaryResourceTable("tb_Mode_Survival", Optional = true)]
public readonly struct SurvivalModeRow
{
    /// <summary>The most spawn boxes a mode names.</summary>
    public const int MaxBoxes = 10;

    public SurvivalModeRow(BinaryReader reader)
    {
        Id = reader.ReadByte();
        Quest = reader.ReadUInt32();
        Maze = reader.ReadUInt32();
        Sector = reader.ReadUInt32();

        // The scene script of the map, which the client runs for itself.
        reader.ReadUtf16UnicodeStringWithoutTerminator();

        SpawnBoxes = new ushort[MaxBoxes];

        for (var i = 0; i < MaxBoxes; i++)
        {
            SpawnBoxes[i] = reader.ReadUInt16();
        }
    }

    /// <summary>The number the script starts the mode with.</summary>
    public byte Id { get; }

    /// <summary>The episode the mode belongs to; nothing reads it.</summary>
    public uint Quest { get; }

    /// <summary>The maze the mode is for.</summary>
    public uint Maze { get; }

    /// <summary>The sector whose monsters have to die for the mode to be won.</summary>
    public uint Sector { get; }

    /// <summary>The spawn boxes that start with the mode, up to the first zero.</summary>
    public ushort[] SpawnBoxes { get; }
}

/// <summary>
/// A mode in which a monster has to be kept alive while waves of monsters come at it.
/// </summary>
[BinaryResourceTable("tb_Mode_Defence", Optional = true)]
public readonly struct DefenceModeRow
{
    public DefenceModeRow(BinaryReader reader)
    {
        Id = reader.ReadByte();
        Quest = reader.ReadUInt32();
        Maze = reader.ReadUInt32();
        Sector = reader.ReadUInt32();
        DefendedMonster = reader.ReadUInt32();
        HateValue = reader.ReadUInt32();
        HateIntervalMilliseconds = reader.ReadUInt32();
        HateRange = reader.ReadUInt16();
        StartBox = reader.ReadUInt32();

        // The scene script of the map, which the client runs for itself.
        reader.ReadUtf16UnicodeStringWithoutTerminator();

        SpawnBoxes = new ushort[SurvivalModeRow.MaxBoxes];

        for (var i = 0; i < SpawnBoxes.Length; i++)
        {
            SpawnBoxes[i] = reader.ReadUInt16();
        }
    }

    /// <summary>The number the script starts the mode with.</summary>
    public byte Id { get; }

    public uint Quest { get; }

    public uint Maze { get; }

    public uint Sector { get; }

    /// <summary>The monster to defend, by its number in the monster table.</summary>
    public uint DefendedMonster { get; }

    /// <summary>How much hate the defended monster gives the monsters around it each time it calls them.</summary>
    public uint HateValue { get; }

    /// <summary>The time between two calls, in milliseconds.</summary>
    public uint HateIntervalMilliseconds { get; }

    /// <summary>How far from the defended monster the call reaches.</summary>
    public ushort HateRange { get; }

    /// <summary>The box the players are put in when the mode starts.</summary>
    public uint StartBox { get; }

    /// <summary>The spawn boxes that start with the mode, up to the first zero.</summary>
    public ushort[] SpawnBoxes { get; }
}

/// <summary>
/// A mode in which a boss, as it loses its life, asks the players to click one of some objects in time.
/// </summary>
[BinaryResourceTable("tb_Mode_Operation", Server = true)]
public readonly struct OperationModeRow
{
    /// <summary>How many shares of its life the boss starts a trial at.</summary>
    public const int Conditions = 5;

    /// <summary>How many messages follow the first one while the trial runs.</summary>
    public const int Reminders = 3;

    /// <summary>How many objects a trial can ask for.</summary>
    public const int MaxObjects = 5;

    public OperationModeRow(BinaryReader reader)
    {
        Id = reader.ReadByte();
        Boss = reader.ReadUInt32();

        LifeConditions = new byte[Conditions];

        for (var i = 0; i < Conditions; i++)
        {
            LifeConditions[i] = reader.ReadByte();
        }

        Maze = reader.ReadUInt16();
        Sector = reader.ReadUInt16();
        Seconds = reader.ReadUInt32();
        StartText = reader.ReadUInt32();

        ReminderSeconds = new ushort[Reminders];

        for (var i = 0; i < Reminders; i++)
        {
            ReminderSeconds[i] = reader.ReadUInt16();
        }

        ReminderTexts = new uint[Reminders];

        for (var i = 0; i < Reminders; i++)
        {
            ReminderTexts[i] = reader.ReadUInt32();
        }

        // The monster the success is for; it is the boss itself in the table.
        reader.ReadUInt32();

        SuccessAura = reader.ReadUInt32();
        FailAura = reader.ReadUInt32();

        Objects = new uint[MaxObjects];

        for (var i = 0; i < MaxObjects; i++)
        {
            Objects[i] = reader.ReadUInt32();
        }

        SuccessText = reader.ReadUInt32();
        FailText = reader.ReadUInt32();
    }

    /// <summary>The number the script starts the mode with.</summary>
    public byte Id { get; }

    /// <summary>The boss, by its number in the monster table.</summary>
    public uint Boss { get; }

    /// <summary>The share of its life, in hundredths, under which the boss starts a trial; the first of them that is not past starts it.</summary>
    public byte[] LifeConditions { get; }

    public ushort Maze { get; }

    /// <summary>The sector of the boss, which the mode takes as the sector of the event.</summary>
    public ushort Sector { get; }

    /// <summary>How long the players have to click the object.</summary>
    public uint Seconds { get; }

    public uint StartText { get; }

    /// <summary>The seconds into a trial at which the following messages are shown.</summary>
    public ushort[] ReminderSeconds { get; }

    public uint[] ReminderTexts { get; }

    /// <summary>The aura the player who clicks in time gets.</summary>
    public uint SuccessAura { get; }

    /// <summary>The aura the boss gets when nobody does.</summary>
    public uint FailAura { get; }

    /// <summary>The interaction boxes of the maze one of which is asked for, up to the first zero.</summary>
    public uint[] Objects { get; }

    public uint SuccessText { get; }

    public uint FailText { get; }
}
