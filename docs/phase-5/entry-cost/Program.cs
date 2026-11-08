// What entering a maze costs in its script: the memory of a LuaState and the time of DoFileAsync, against a chunk compiled
// once and run in each new state, and against one state with a table of globals per instance.
// dotnet run -c Release -- <folder of the maze scripts>

using System.Diagnostics;
using System.Globalization;
using Lua;
using Lua.Runtime;
using Lua.Standard;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Lua;

var folder = args[0];
var files = Directory.GetFiles(folder, "*.lua").OrderBy(e => e, StringComparer.Ordinal).ToArray();
var typical = files.First(e => Path.GetFileName(e) == "M04_RSQUARE_EP_01_Normal.lua");
var largest = files.OrderByDescending(e => new FileInfo(e).Length).First();

static LuaState NewState()
{
    var state = LuaState.Create();

    state.OpenStandardLibraries();
    LuaCompatibility.Apply(state);

    state.Environment["Soulworker"] = new LuaValue(new LuaTable
    {
        ["GetHelper"] = new LuaFunction((context, _) =>
        {
            context.Return(new LuaGameHelper(NullLogger<LuaGameHelper>.Instance));
            return ValueTask.FromResult(1);
        })
    });

    return state;
}

static long Settled()
{
    for (var i = 0; i < 3; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    return GC.GetTotalMemory(true);
}

static string Kb(double bytes) => (bytes / 1024).ToString("F1", CultureInfo.InvariantCulture) + " KB";
static string Us(double ticks) => (ticks * 1_000_000d / Stopwatch.Frequency).ToString("F0", CultureInfo.InvariantCulture) + " us";

const int Count = 200;

// Memory of a state alone, and of a state with the script of a maze run in it.
var keep = new List<object>(Count);
var before = Settled();

for (var i = 0; i < Count; i++)
{
    keep.Add(NewState());
}

var empty = (Settled() - before) / (double)Count;

keep.Clear();
before = Settled();

foreach (var path in new[] { typical, largest })
{
    keep.Clear();
    before = Settled();

    for (var i = 0; i < Count; i++)
    {
        var state = NewState();

        await state.DoFileAsync(path);

        keep.Add(state);
    }

    Console.WriteLine($"memory, state with {Path.GetFileName(path)} ({new FileInfo(path).Length / 1024} KB of source): {Kb((Settled() - before) / (double)Count)} per state; empty state {Kb(empty)}");
}

keep.Clear();

// Time per entry, every script of the client: a new state and DoFileAsync, as PersonService does.
var compiled = new Dictionary<string, Prototype>();

foreach (var path in files)
{
    compiled[path] = (await NewState().LoadFileAsync(path, "bt", null, CancellationToken.None)).Proto;
}

async Task<(double Ticks, double Bytes)> Measure(Func<string, Task> entry, int rounds)
{
    // One round warms the JIT up.
    foreach (var path in files)
    {
        await entry(path);
    }

    var allocated = GC.GetAllocatedBytesForCurrentThread();
    var started = Stopwatch.GetTimestamp();

    for (var round = 0; round < rounds; round++)
    {
        foreach (var path in files)
        {
            await entry(path);
        }
    }

    var entries = (double)rounds * files.Length;

    return ((Stopwatch.GetTimestamp() - started) / entries, (GC.GetAllocatedBytesForCurrentThread() - allocated) / entries);
}

var stateOnly = await Measure(_ => { NewState(); return Task.CompletedTask; }, 10);
var doFile = await Measure(async path => await NewState().DoFileAsync(path), 10);
var cached = await Measure(async path =>
{
    var state = NewState();

    await state.ExecuteAsync(new LuaClosure(state, compiled[path], state.Environment));
}, 10);

// One state for every maze of a script, each instance with a table of its own globals that falls back on the shared ones.
var shared = NewState();
var sharedTime = await Measure(async path =>
{
    var globals = new LuaTable { Metatable = new LuaTable { ["__index"] = shared.Environment } };

    await shared.ExecuteAsync(new LuaClosure(shared, compiled[path], globals));
}, 10);

Console.WriteLine($"entries measured: {files.Length} scripts x 10 rounds");
Console.WriteLine($"new state only:              {Us(stateOnly.Ticks)}, {Kb(stateOnly.Bytes)} allocated");
Console.WriteLine($"new state + DoFileAsync:     {Us(doFile.Ticks)}, {Kb(doFile.Bytes)} allocated");
Console.WriteLine($"new state + compiled chunk:  {Us(cached.Ticks)}, {Kb(cached.Bytes)} allocated");
Console.WriteLine($"shared state + own globals:  {Us(sharedTime.Ticks)}, {Kb(sharedTime.Bytes)} allocated");

// Memory of an instance on a shared state.
var tables = new List<LuaTable>(Count);

before = Settled();

for (var i = 0; i < Count; i++)
{
    var globals = new LuaTable { Metatable = new LuaTable { ["__index"] = shared.Environment } };

    await shared.ExecuteAsync(new LuaClosure(shared, compiled[typical], globals));

    tables.Add(globals);
}

Console.WriteLine($"memory, instance of {Path.GetFileName(typical)} on a shared state: {Kb((Settled() - before) / (double)Count)} per instance");

GC.KeepAlive(tables);
GC.KeepAlive(shared);
