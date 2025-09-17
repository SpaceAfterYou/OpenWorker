using System.Text.Json;
using OpenWorker.SourceGenerator.ResourceStructures;

// Solution directory
var root = Directory
    .GetParent(Directory.GetCurrentDirectory())?
    .Parent?
    .Parent?
    .Parent?
    .FullName;

if (Directory.Exists(root) is false)
{
    throw new DirectoryNotFoundException();
}

await using var stream = File.OpenRead("Structures.json");

var tables = await JsonSerializer
    .DeserializeAsync<Dictionary<string, List<string>>>(stream)
    .ConfigureAwait(false);

ArgumentNullException.ThrowIfNull(tables);

await Parallel.ForEachAsync(tables, async (table, ct) =>
{
    var (tableName, tableTypes) = table;
    var types = tableTypes.Select(e => Type.GetType($"System.{e}", true)!).ToArray();

    var name = NameHelper.GetClassName(tableName);

    var modelName = $"Resource{name}";

    await FileHelper.WriteRes(root, tableName, modelName, $"{name}Row", types, ct);
    // await FileHelper.WriteSeed(root, tableName, modelName, $"{name}Row", types, ct);
    // await FileHelper.WriteModel(root, tableName, $"Resource{name}", types, ct);
});