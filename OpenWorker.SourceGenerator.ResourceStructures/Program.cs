using OpenWorker.SourceGenerator.ResourceStructures;

// Writes the rows of the client tables (OpenWorker.Res/Rows) from their definition, Structures.json, which is the one place
// their columns are declared:
//
//     dotnet run --project OpenWorker.SourceGenerator.ResourceStructures [-- <root of the repository>]
//
// ResourceRowsTest fails when a row differs from what this writes.

var root = args.Length > 0 ? args[0] : RowWriter.FindRoot(AppContext.BaseDirectory);
var folder = Path.Join([root, .. RowWriter.Folder]);

foreach (var table in TableDefinition.Load(Path.Join([root, .. RowWriter.Definition])))
{
    await File.WriteAllTextAsync(Path.Join(folder, table.RowName + ".cs"), RowWriter.Render(table));
}
