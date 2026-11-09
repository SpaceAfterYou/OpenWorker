using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// Where a new character of a hero starts, from the server's own <c>tb_Character_Info</c>: the tutorial maze of the
/// prologue and its start box, the quest the prologue begins with and the one that stands for the whole prologue when it
/// is left. The client's table has the same rows without these columns. The row of a hero is the one of
/// <c>1000 * hero</c>.
/// </summary>
[BinaryResourceTable("tb_Character_Info", Server = true)]
public readonly struct CharacterStartRow
{
    private const int Skills = 20;
    private const int QuickSlots = 20;
    private const int Gestures = 6;

    public CharacterStartRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();

        // The hero and its group, then the name of its model.
        reader.ReadByte();
        reader.ReadUInt16();
        reader.ReadBytes(reader.ReadUInt16() * sizeof(char));

        // The weapon and the four default clothes, then the item given at creation.
        reader.ReadBytes(5 * sizeof(int) + sizeof(ushort));

        District = reader.ReadInt16();

        // The place in the district.
        reader.ReadBytes(3 * sizeof(short));

        Maze = reader.ReadInt16();
        MazeStart = reader.ReadInt32();
        StartQuest = reader.ReadInt32();
        SkipQuest = reader.ReadInt32();

        // The status, the height, six speeds and the ratio of moving back, the faction, the size of the body, the
        // default skills, the quick slots and the gestures, the time of the akashic records, and two last bytes.
        reader.ReadBytes(sizeof(byte) + sizeof(int) + sizeof(short) + 6 * sizeof(ushort) + sizeof(float) + sizeof(byte) + 3 * sizeof(ushort));
        reader.ReadBytes(Skills * sizeof(int) + QuickSlots + Gestures * sizeof(ushort) + sizeof(float) + 2);
    }

    public ushort Id { get; }

    /// <summary>
    /// The district the character starts in.
    /// </summary>
    public short District { get; }

    /// <summary>
    /// The first tutorial maze, where the prologue starts; 0 for the rows of the character selection.
    /// </summary>
    public short Maze { get; }

    /// <summary>
    /// The start box of that maze.
    /// </summary>
    public int MazeStart { get; }

    /// <summary>
    /// The quest episode the prologue begins with.
    /// </summary>
    public int StartQuest { get; }

    /// <summary>
    /// The quest episode that counts the prologue as done: leaving the tutorial finishes it, so the story goes on from it.
    /// </summary>
    public int SkipQuest { get; }
}
