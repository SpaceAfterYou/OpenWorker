namespace OpenWorker.Extensions.Data;

/// <summary>
/// Every folder of game data a server reads. The readers take their paths from here, and each server names the ones it
/// needs (<see cref="DataNeed"/>), so the start of a server and the container files can be checked against one list.
/// </summary>
public static class DataFolders
{
    private const string ServerTablesPath = "res-server";

    /// <summary>The client tables, <c>bin/Table</c> of the client's data.</summary>
    public static DataFolder ClientTables { get; } = DataFolder.Of("res", "*.res", "client tables (*.res)");

    /// <summary>
    /// The tables only the original server has. The folder of the client tables put in its place holds none of them, so
    /// it counts as filled only with a table the client tables do not have.
    /// </summary>
    public static DataFolder ServerTables { get; } = new(ServerTablesPath, "server-only tables (*.res the client tables do not have)", HoldsServerTables);

    /// <summary>The maps, <c>World/Table</c> of the client's data.</summary>
    public static DataFolder Maps { get; } = DataFolder.Of("batch", "*.vbatch", "maps (*.vbatch)");

    /// <summary>The navigation meshes of the maps, <c>World/Navmesh</c> of the client's data.</summary>
    public static DataFolder NavMeshes { get; } = DataFolder.Of("navmesh", "*.hkt", "navigation meshes (*.hkt)");

    /// <summary>The action files of heroes and monsters, <c>Character/ActionData</c> of the client's data.</summary>
    public static DataFolder Actions { get; } = DataFolder.Of("action", "*.adf", "action files (*.adf)");

    /// <summary>The scripts of the mazes, <c>Scripts/Server</c> of the client's data.</summary>
    public static DataFolder MazeScripts { get; } = DataFolder.Of("scripts", "*.lua", "maze scripts (*.lua)");

    /// <summary>The minds of monsters and helpers, <c>Scripts/AI</c> of the client's data.</summary>
    public static DataFolder MonsterMinds { get; } = DataFolder.Of("scripts/AI", "*.lua", "minds of monsters (*.lua)");

    public static IReadOnlyList<DataFolder> All { get; } = [ClientTables, ServerTables, Maps, NavMeshes, Actions, MazeScripts, MonsterMinds];

    private static bool HoldsServerTables(string root)
    {
        var client = Path.Join(root, ClientTables.Path);

        return DataFolder.Files(Path.Join(root, ServerTablesPath), "*.res").Any(e => !File.Exists(Path.Join(client, Path.GetFileName(e))));
    }
}
