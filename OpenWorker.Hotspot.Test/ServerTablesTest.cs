using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The tables that only the server has are read to their last row: set OPENWORKER_TEST_SERVER_RES to the folder with them.
/// </summary>
[TestClass]
public sealed class ServerTablesTest
{
    // What follows the rows in the files, it is not part of any row.
    private const int Trailer = 34;

    private static (int Count, long Remaining) Read(string table, Type row)
    {
        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_SERVER_RES");

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_SERVER_RES is not set.");
        }

        using var stream = File.OpenRead(Path.Combine(folder, table + ".res"));
        using var reader = new BinaryReader(stream);

        var count = reader.ReadInt32();

        for (var i = 0; i < count; i++)
        {
            Activator.CreateInstance(row, reader);
        }

        return (count, stream.Length - stream.Position);
    }

    /// <summary>
    /// Every hand-written row layout of a server table reads its file to the trailer: a column too many or too few would
    /// stop short of it or run past it.
    /// </summary>
    [TestMethod]
    public void RowsFillTheFileUpToTheTrailer()
    {
        (string Table, Type Row)[] tables =
        [
            ("tb_Monster_EXP", typeof(MonsterExpRow)),
            ("tb_PartyExp_Mob", typeof(PartyExpMobRow)),
            ("tb_PartyExp_Level", typeof(PartyExpLevelRow)),
            ("tb_DropRate_Mob", typeof(DropRateMobRow)),
            ("tb_Drop", typeof(DropRow)),
            ("tb_Drop_Group", typeof(DropGroupRow)),
            ("tb_MazeReward_Item", typeof(MazeRewardItemRow)),
            ("tb_Maze_Info", typeof(MazeRuleRow)),
            ("tb_Maze_OpenControl", typeof(MazeOpenControlRow)),
            ("tb_DailyMaze_Portal", typeof(MazeDailyPortalRow)),
            ("tb_MazeReward_Standard", typeof(MazeRewardStandardRow)),
            ("tb_MazeReward_Rank", typeof(MazeRankShareRow)),
            ("tb_MazeReward_Time", typeof(MazeTimeScoreRow)),
            ("tb_MazeReward_Difficulty", typeof(MazeDifficultyScoreRow)),
            ("tb_MazeReward_Level", typeof(MazeLevelScoreRow)),
            ("tb_MazeReward_Normal", typeof(MazePlayScoreRow)),
            ("tb_MazeReward_PartyValue", typeof(MazePartyShareRow)),
            ("tb_MazeReward_Revision", typeof(MazeComboRevisionRow)),
            ("tb_Reinforce", typeof(ReinforceChanceRow)),
            ("tb_Buff", typeof(BuffSystemRow)),
            ("tb_Disassemble", typeof(DisassembleResultRow)),
            ("TB_BroachCompose_Rank", typeof(BroachComposeRankRow)),
            ("TB_BroachCompose_Type", typeof(BroachComposeTypeRow)),
            ("TB_BroachCompose_Theme", typeof(BroachComposeThemeRow)),
            ("TB_BroachData", typeof(BroachComposeItemRow)),
            ("tb_Achievement", typeof(AchievementTargetRow)),
            ("tb_DS_Point", typeof(SharePointRow)),
            ("tb_Character_Info", typeof(CharacterStartRow)),
            ("tb_Levelup_Point", typeof(LevelSkillPointRow)),
            ("tb_Status", typeof(StatusSoulGaugeRow)),
            ("tb_Soul_Guage", typeof(SoulGaugeRuleRow)),
            ("tb_Random_Option", typeof(ItemOptionRollRow)),
            ("tb_Socket", typeof(SocketChanceRow)),
            ("tb_Item_Endurance", typeof(ItemEnduranceRow))
        ];

        foreach (var (table, row) in tables)
        {
            var (count, rest) = Read(table, row);

            Assert.IsGreaterThan(0, count, table);
            Assert.AreEqual(Trailer, rest, table);
        }
    }
}
