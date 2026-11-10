using OpenWorker.Extensions;
using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The server's own <c>tb_Maze_Info</c>: what the client's copy of a maze leaves out. The time a run is measured against,
/// the experience and gold a clear is worth before its rank, the maze that must be cleared first, the quest and the item a
/// maze asks for, how many times a day it may be entered, whether and how often a fallen player may get up in it, and how
/// hard it is. The rest of the row is read to reach its end.
/// </summary>
[BinaryResourceTable("tb_Maze_Info", Server = true)]
public readonly struct MazeRuleRow
{
    public MazeRuleRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();
        Type = reader.ReadByte();
        Group = reader.ReadUInt16();
        RequiredLevel = reader.ReadByte();
        Admission = reader.ReadByte();
        Fatigue = reader.ReadUInt16();

        // The daily limit of entering: the group of mazes that share it, whether it is counted for the account or for the
        // character, and the count; then the count in a net cafe, the days that add more, how many they add and the count of
        // helpers, which a server without a net cafe has no use for.
        EntryLimitGroup = reader.ReadUInt16();
        reader.ReadByte();
        EntryLimit = reader.ReadByte();
        reader.ReadBytes(4);

        RequiredQuest = reader.ReadUInt32();
        RequiredItem = reader.ReadUInt32();
        RequiredItemCount = reader.ReadByte();

        // Whether a player may get up in the maze, and how many revive items it may use there.
        RevivalType = reader.ReadByte();
        RevivalItemCount = reader.ReadByte();

        Difficulty = reader.ReadByte();

        // The layers of the map.
        reader.ReadByte();

        Episode = reader.ReadByte();

        // How the maze is cleared: the kind of the calculation, three conditions and their values.
        reader.ReadBytes(1 + 3 + 3 * sizeof(uint));

        RequiredMaze = reader.ReadUInt32();

        // The maze and the group a clear opens.
        reader.ReadBytes(2 * sizeof(ushort));

        ClearTime = reader.ReadUInt16();
        RewardExperience = reader.ReadUInt32();
        RewardGold = reader.ReadUInt32();

        // The rate of binding, the map and the script of the server.
        reader.ReadUInt16();
        reader.ReadUtf16UnicodeStringWithoutTerminator();
        reader.ReadUtf16UnicodeStringWithoutTerminator();

        // The monster of the attacks of the scene, the share of the points and the name.
        reader.ReadUInt32();
        reader.ReadSingle();
        reader.ReadUtf16UnicodeStringWithoutTerminator();

        // The portal the maze is entered by, then the place and the size, the way out, the quests that run in it and the
        // akashic records.
        PortalId = reader.ReadUInt16();
        reader.ReadBytes(4 * sizeof(int) + sizeof(short) + sizeof(uint) + 5 * sizeof(uint) + 1);
        PotionLimit = reader.ReadByte();

        // The warp orb and whether the maze can be entered again.
        reader.ReadBytes(sizeof(uint) + 1);
    }

    public ushort Id { get; }

    /// <summary>
    /// 1 when the maze does not let its players use items (potions and the like); 0 for any other.
    /// </summary>
    public byte PotionLimit { get; }

    /// <summary>
    /// The kind of the maze, as in the client's table (0 a plain maze, 1 a tutorial, 7 the infinite tower, ...).
    /// </summary>
    public byte Type { get; }

    public ushort Group { get; }

    /// <summary>
    /// The lowest level that may enter; 0 for any.
    /// </summary>
    public byte RequiredLevel { get; }

    /// <summary>
    /// Who may enter: 0 anybody, 1 a player alone, 2 a party, 3 a full party, 4 nobody.
    /// </summary>
    public byte Admission { get; }

    /// <summary>
    /// The fatigue the maze takes, half when the player comes in and half at the clear.
    /// </summary>
    public ushort Fatigue { get; }

    /// <summary>
    /// How many times a day a person may enter; 0 for no limit. Mazes of one <see cref="EntryLimitGroup"/> share the count.
    /// </summary>
    public byte EntryLimit { get; }

    /// <summary>
    /// The group of mazes that share the daily count of entering, 0 for a maze that counts alone.
    /// </summary>
    public ushort EntryLimitGroup { get; }

    /// <summary>
    /// What a fallen player may do in the maze: 0 get up, 1 not get up at all, 2 get up as often as it likes, by itself.
    /// </summary>
    public byte RevivalType { get; }

    /// <summary>
    /// How many revive items a player may use in the maze; 0 for as many as it has.
    /// </summary>
    public byte RevivalItemCount { get; }

    /// <summary>
    /// The portal the maze is entered by; the casual raids of a portal take turns by the day of the week.
    /// </summary>
    public ushort PortalId { get; }

    /// <summary>
    /// The quest (an episode) the person must have or have finished; 0 for none.
    /// </summary>
    public uint RequiredQuest { get; }

    /// <summary>
    /// The item the person must carry; 0 for none.
    /// </summary>
    public uint RequiredItem { get; }

    public byte RequiredItemCount { get; }

    /// <summary>
    /// How hard the maze is: 0 normal, then harder.
    /// </summary>
    public byte Difficulty { get; }

    public byte Episode { get; }

    /// <summary>
    /// The maze the person must have cleared first; 0 for none.
    /// </summary>
    public uint RequiredMaze { get; }

    /// <summary>
    /// The seconds a run is measured against: the time left of it adds to the score.
    /// </summary>
    public ushort ClearTime { get; }

    /// <summary>
    /// The experience of a clear, before the rank and the party share it out.
    /// </summary>
    public uint RewardExperience { get; }

    /// <summary>
    /// The gold of a clear, before the rank and the party share it out.
    /// </summary>
    public uint RewardGold { get; }
}
