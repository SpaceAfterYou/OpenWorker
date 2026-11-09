using Lua;
using Lua.Standard;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class LuaCompatibilityTest
{
    private static async Task<int> RunAsync(string code)
    {
        var state = LuaState.Create();

        state.OpenStandardLibraries();

        LuaCompatibility.Apply(state);

        var result = await state.DoStringAsync(code);

        return result[0].Read<int>();
    }

    [TestMethod]
    public async Task ScriptsOfTheOldLibraryCountTheItemsOfATable()
    {
        Assert.AreEqual(3, await RunAsync("return table.getn({ 10, 20, 30 })"));
        Assert.AreEqual(0, await RunAsync("return table.getn({})"));
    }

    [TestMethod]
    public async Task ScriptsBuildTheVectorsOfTheEngine()
    {
        Assert.AreEqual(39987, await RunAsync("local v = Vision.hkvVec3(39987.5, 20082.9, 99.7) return math.floor(v.x)"));
        Assert.AreEqual(99, await RunAsync("local v = Vision.hkvVec3(1, 2, 99.7) return math.floor(v.z)"));
    }
}
