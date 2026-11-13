using System.Collections.Concurrent;
using Lua;
using Lua.Runtime;

namespace OpenWorker.Lua;

/// <summary>
/// The scripts of the mazes, each read and compiled once, off the world loop, and run in the state of every maze that
/// uses it. A compiled chunk does not change when it runs, so the mazes share it: entering a maze runs the chunk in a
/// new state instead of reading and compiling the file again.
/// </summary>
/// <param name="folder">Where the scripts of the client are, by name.</param>
public sealed class MazeScripts(string folder = "scripts")
{
    private readonly ConcurrentDictionary<string, Lazy<Task<Prototype?>>> _compiled = new(StringComparer.Ordinal);

    /// <summary>
    /// The compiled script of the name, or nothing when the client has no such file. Everyone who asks while it is being
    /// read waits for the same reading. A script that does not compile fails here, every time it is asked for.
    /// </summary>
    public Task<Prototype?> GetAsync(string name)
    {
        return _compiled.GetOrAdd(name, e => new Lazy<Task<Prototype?>>(() => Task.Run(() => CompileAsync(e)))).Value;
    }

    /// <summary>
    /// Runs the script of the name in <paramref name="state"/>, as reading the file there would. False when the client
    /// has no such script.
    /// </summary>
    public async ValueTask<bool> RunAsync(LuaState state, string name, CancellationToken cancellationToken)
    {
        if (await GetAsync(name) is not { } chunk)
        {
            return false;
        }

        await state.ExecuteAsync(new LuaClosure(state, chunk, state.Environment), cancellationToken);

        return true;
    }

    /// <summary>
    /// Where the script of a name is read from.
    /// </summary>
    public string PathOf(string name) => Path.Join(folder, $"{name}.lua");

    private async Task<Prototype?> CompileAsync(string name)
    {
        var path = PathOf(name);

        if (!File.Exists(path))
        {
            return null;
        }

        // The state only compiles: the chunk is run in the state of each maze.
        var compiler = LuaState.Create();

        var closure = await compiler.LoadFileAsync(path, "bt", null, CancellationToken.None);

        return closure.Proto;
    }
}
