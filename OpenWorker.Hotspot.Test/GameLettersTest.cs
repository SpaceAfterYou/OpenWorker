using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The client writes a letter of the game from the row it names, so a wrong row is silent on the server and wrong in the
/// client. Set OPENWORKER_TEST_RES to the client tables to check the rows against the client's own.
/// </summary>
[TestClass]
public sealed class GameLettersTest
{
    [TestMethod]
    public void EveryLetterOfTheGameNamesTheRowOfWhatItSays()
    {
        // Which letter names which row, and the main and sub type of that row in the client.
        (string Letter, byte Row, byte Expected, short Main, short Sub)[] letters =
        [
            ("text of the server", GameLetters.OwnText, 9, 4, 1),
            ("exchange: bought", GameLetters.ExchangeBought, 5, 2, 1),
            ("exchange: sold", GameLetters.ExchangeSold, 6, 2, 2),
            ("exchange: taken back", GameLetters.ExchangeTakenBack, 7, 2, 3),
            ("exchange: ended", GameLetters.ExchangeEnded, 16, 2, 4),
            ("exchange: sold without commission", GameLetters.ExchangeSoldWithoutCommission, 18, 2, 5),
            ("maze reward kept", GameLetters.MazeRewardKept, 11, 5, 1),
            ("daily mission reward", GameLetters.DailyMissionReward, 12, 5, 2)
        ];

        foreach (var letter in letters)
        {
            Assert.AreEqual(letter.Expected, letter.Row, letter.Letter);
        }

        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES");

        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Combine(folder, "tb_SystemMail.res")));
        var rows = Enumerable.Range(0, reader.ReadInt32()).Select(_ => new SystemMailRow(reader)).ToDictionary(e => e.Id);

        foreach (var letter in letters)
        {
            Assert.IsTrue(rows.TryGetValue(letter.Row, out var row), $"{letter.Letter}: the client has row {letter.Row}");
            Assert.AreEqual((letter.Main, letter.Sub), (row.Field1, row.Field2), letter.Letter);
        }
    }
}
