using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The lookups of the tables went from a search from the top of a table to a dictionary made once: the row found must be
/// the one the search found, the first of its key, and a key the table does not have must be an answer, not an exception.
/// </summary>
[TestClass]
public sealed class RowIndexTest
{
    private static LeagueInfoRow Row(int id, int rooms)
    {
        var bytes = new byte[1024];

        BitConverter.TryWriteBytes(bytes, id);
        BitConverter.TryWriteBytes(bytes.AsSpan(3 * sizeof(int)), rooms);

        return new LeagueInfoRow(new BinaryReader(new MemoryStream(bytes)));
    }

    [TestMethod]
    public void TheFirstRowOfAKeyWinsAndAMissingKeyIsAnAnswer()
    {
        LeagueInfoRow[] table = [Row(1, 30), Row(2, 40), Row(2, 50)];

        var index = table.IndexBy(e => e.Id);

        Assert.AreEqual(40, index.GetOrDefault(2).MemberLimit);
        Assert.IsFalse(index.TryGet(3, out _));
        Assert.AreEqual(0, index.GetOrDefault(3).Id);
    }
}
