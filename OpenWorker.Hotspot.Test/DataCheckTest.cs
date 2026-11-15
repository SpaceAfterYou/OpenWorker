using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenWorker.DependencyInjection;
using OpenWorker.Extensions.Data;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A server says at its start which folder of game data it lacks and what will not work, and does not start without
/// the folders it cannot play without.
/// </summary>
[TestClass]
public sealed class DataCheckTest
{
    private static readonly IConfiguration NoSettings = new ConfigurationBuilder().Build();

    [TestMethod]
    public void AnEmptyFolderTheServerCannotPlayWithoutStopsTheStart()
    {
        using var root = new TempRoot();
        Directory.CreateDirectory(Path.Join(root.Path, "scripts", "AI"));
        File.WriteAllText(Path.Join(root.Path, "scripts", "C01.lua"), string.Empty);
        var logger = new FakeLogger();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            DataCheck.Run([new DataNeed(DataFolders.MazeScripts, "no script"), new DataNeed(DataFolders.MonsterMinds, "monsters stand still", Refuse: true)],
                NoSettings, logger, root.Path));

        var line = logger.Collector.GetSnapshot().Single();
        Assert.AreEqual(LogLevel.Error, line.Level);
        StringAssert.Contains(line.Message, Path.Join("scripts", "AI"));
        StringAssert.Contains(line.Message, "monsters stand still");
    }

    [TestMethod]
    public void AMissingFolderTheServerCanPlayWithoutIsAnErrorAndTheStartGoesOn()
    {
        using var root = new TempRoot();
        var logger = new FakeLogger();

        DataCheck.Run([new DataNeed(DataFolders.NavMeshes, "monsters walk through walls")], NoSettings, logger, root.Path);

        Assert.AreEqual(LogLevel.Error, logger.Collector.GetSnapshot().Single().Level);
    }

    [TestMethod]
    public void AFolderOfASettingThatIsOffIsNotChecked()
    {
        using var root = new TempRoot();
        var logger = new FakeLogger();
        var prologue = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Game:Prologue"] = "false" }).Build();

        DataCheck.Run([new DataNeed(DataFolders.Maps, "no prologue", Refuse: true, When: e => e.GetValue("Game:Prologue", true))], prologue, logger, root.Path);

        Assert.AreEqual(0, logger.Collector.Count);
    }

    [TestMethod]
    public void TheClientTablesInThePlaceOfTheServerTablesAreNoServerTables()
    {
        using var root = new TempRoot();
        Directory.CreateDirectory(Path.Join(root.Path, "res"));
        Directory.CreateDirectory(Path.Join(root.Path, "res-server"));
        File.WriteAllText(Path.Join(root.Path, "res", "tb_Item.res"), string.Empty);
        File.WriteAllText(Path.Join(root.Path, "res-server", "tb_Item.res"), string.Empty);

        Assert.IsFalse(DataFolders.ServerTables.IsFilledIn(root.Path));

        File.WriteAllText(Path.Join(root.Path, "res-server", "tb_Drop.res"), string.Empty);

        Assert.IsTrue(DataFolders.ServerTables.IsFilledIn(root.Path));
    }

    private sealed class TempRoot : IDisposable
    {
        private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("openworker-data-");

        public string Path => _folder.FullName;

        public void Dispose() => _folder.Delete(recursive: true);
    }
}
