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

    private static (int Count, long Remaining) Read<T>(string table)
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
            Activator.CreateInstance(typeof(T), reader);
        }

        return (count, stream.Length - stream.Position);
    }

    [TestMethod]
    [DataRow("tb_Monster_EXP", typeof(MonsterExpRow))]
    [DataRow("tb_PartyExp_Mob", typeof(PartyExpMobRow))]
    [DataRow("tb_DropRate_Mob", typeof(DropRateMobRow))]
    [DataRow("tb_Drop", typeof(DropRow))]
    [DataRow("tb_Drop_Group", typeof(DropGroupRow))]
    public void RowsFillTheFileUpToTheTrailer(string table, Type row)
    {
        var read = typeof(ServerTablesTest)
            .GetMethod(nameof(Read), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(row);

        var (count, rest) = ((int, long))read.Invoke(null, [table])!;

        Assert.IsGreaterThan(0, count);
        Assert.AreEqual(Trailer, rest);
    }
}
