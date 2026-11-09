using Lua;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A maze script is compiled once and run in the state of every maze: the mazes share the code, never the globals.
/// </summary>
[TestClass]
public sealed class MazeScriptsTest
{
    [TestMethod]
    public async Task ScriptCompiledOnceRunsInEveryStateWithGlobalsOfItsOwn()
    {
        var folder = Directory.CreateTempSubdirectory();

        try
        {
            await File.WriteAllTextAsync(Path.Join(folder.FullName, "Maze.lua"), "entered = (entered or 0) + 1");

            var scripts = new MazeScripts(folder.FullName);
            var first = LuaState.Create();
            var second = LuaState.Create();

            Assert.IsTrue(await scripts.RunAsync(first, "Maze", CancellationToken.None));
            Assert.IsTrue(await scripts.RunAsync(second, "Maze", CancellationToken.None));
            Assert.IsFalse(await scripts.RunAsync(first, "Missing", CancellationToken.None));

            Assert.AreSame(await scripts.GetAsync("Maze"), await scripts.GetAsync("Maze"));
            Assert.AreEqual(1d, first.Environment["entered"].Read<double>());
            Assert.AreEqual(1d, second.Environment["entered"].Read<double>());
        }

        finally
        {
            folder.Delete(true);
        }
    }
}
