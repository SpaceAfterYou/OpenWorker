using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

// The server's own copies of the reward tables of a maze. The client's copies of these tables keep only the key of each row;
// the values are in the server's, read from res-server/ only.

/// <summary>
/// What a rank of a maze gives: the share of the experience and of the gold of the maze, by rank (0 is F, 5 is SS).
/// </summary>
[BinaryResourceTable("tb_MazeReward_Rank", Server = true)]
public readonly struct MazeRankShareRow(BinaryReader reader)
{
    public byte Id { get; } = reader.ReadByte();
    public uint ScoreMin { get; } = reader.ReadUInt32();
    public uint ScoreMax { get; } = reader.ReadUInt32();
    public byte Rank { get; } = reader.ReadByte();
    public float Experience { get; } = reader.ReadSingle();
    public float Gold { get; } = reader.ReadSingle();
    public float SharePoint { get; } = reader.ReadSingle();
}

/// <summary>
/// What the time left of a run adds to its score: each step of <see cref="Step"/> seconds left, up to <see cref="MaxSteps"/>,
/// adds <see cref="Value"/> to a factor of one. The first row is the one that counts.
/// </summary>
[BinaryResourceTable("tb_MazeReward_Time", Server = true)]
public readonly struct MazeTimeScoreRow(BinaryReader reader)
{
    public ushort Id { get; } = reader.ReadUInt16();
    public ushort Step { get; } = reader.ReadUInt16();
    public ushort MaxSteps { get; } = reader.ReadUInt16();
    public float Value { get; } = reader.ReadSingle();
}

/// <summary>
/// The factor of the score of a maze by how hard it is: the row is the difficulty plus one.
/// </summary>
[BinaryResourceTable("tb_MazeReward_Difficulty", Server = true)]
public readonly struct MazeDifficultyScoreRow(BinaryReader reader)
{
    public byte Id { get; } = reader.ReadByte();
    public byte Difficulty { get; } = reader.ReadByte();
    public float Value { get; } = reader.ReadSingle();
}

/// <summary>
/// The factor of the score by how far the level of the player is above the level of the maze: the row is that gap.
/// </summary>
[BinaryResourceTable("tb_MazeReward_Level", Server = true)]
public readonly struct MazeLevelScoreRow(BinaryReader reader)
{
    public byte Id { get; } = reader.ReadByte();
    public byte Level { get; } = reader.ReadByte();
    public float Value { get; } = reader.ReadSingle();
}

/// <summary>
/// The points of what a player did in a run, by how much of it: the row is the count (of the longest combo, of the share of
/// the damage in percent, of the hits taken).
/// </summary>
[BinaryResourceTable("tb_MazeReward_Normal", Server = true)]
public readonly struct MazePlayScoreRow(BinaryReader reader)
{
    public ushort Id { get; } = reader.ReadUInt16();
    public uint Count { get; } = reader.ReadUInt32();

    /// <summary>The points of the longest combo.</summary>
    public uint Combo { get; } = reader.ReadUInt32();

    private readonly uint _climax = reader.ReadUInt32();
    private readonly uint _erosion = reader.ReadUInt32();
    private readonly uint _collaboration = reader.ReadUInt32();

    /// <summary>The points of the share of the damage the party did.</summary>
    public uint Damage { get; } = reader.ReadUInt32();

    /// <summary>The percent of the score kept after this many hits taken.</summary>
    public uint HitsTakenRate { get; } = reader.ReadUInt32();
}

/// <summary>
/// What a member of a party gets on top of the share of its rank, by the size of the party and the place of the member
/// in the order of the scores.
/// </summary>
[BinaryResourceTable("tb_MazeReward_PartyValue", Server = true)]
public readonly struct MazePartyShareRow
{
    public const int Places = 4;

    public MazePartyShareRow(BinaryReader reader)
    {
        Id = reader.ReadByte();
        Members = reader.ReadByte();
        Experience = new float[Places];
        Gold = new float[Places];

        for (var i = 0; i < Places; i++)
        {
            Experience[i] = reader.ReadSingle();
        }

        for (var i = 0; i < Places; i++)
        {
            Gold[i] = reader.ReadSingle();
        }
    }

    public byte Id { get; }
    public byte Members { get; }

    /// <summary>The share of experience, by place from the first.</summary>
    public float[] Experience { get; }

    /// <summary>The share of gold, by place from the first.</summary>
    public float[] Gold { get; }
}

/// <summary>
/// The factor of the combo of each hero: a hero whose hits come slower counts its combo higher.
/// </summary>
[BinaryResourceTable("tb_MazeReward_Revision", Optional = true)]
public readonly struct MazeComboRevisionRow(BinaryReader reader)
{
    public ushort Hero { get; } = reader.ReadUInt16();
    public float Value { get; } = reader.ReadSingle();
    private readonly float _other = reader.ReadSingle();
}
