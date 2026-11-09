using OpenWorker.SourceGenerator.ResourceStructures;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The columns of the client tables are declared in one place, <c>Structures.json</c>, and the rows of
/// <c>OpenWorker.Res/Rows</c> are what the generator writes from it. A column named or typed by hand in a row, or a row the
/// definition does not have, fails here: change the definition and run the generator
/// (<c>dotnet run --project OpenWorker.SourceGenerator.ResourceStructures</c>). What a row offers beyond its columns is
/// written in a partial struct of <c>OpenWorker.Res/Members</c>.
/// </summary>
[TestClass]
public sealed class ResourceRowsTest
{
    [TestMethod]
    public void TheRowsAreWhatTheirDefinitionWrites()
    {
        var root = RowWriter.FindRoot(AppContext.BaseDirectory);
        var folder = Path.Join([root, .. RowWriter.Folder]);
        var tables = TableDefinition.Load(Path.Join([root, .. RowWriter.Definition]));

        var differing = tables
            .Where(table =>
            {
                var path = Path.Join(folder, table.RowName + ".cs");

                return !File.Exists(path) || File.ReadAllText(path).ReplaceLineEndings("\n") != RowWriter.Render(table);
            })
            .Select(e => e.RowName)
            .ToArray();

        var undefined = Directory.GetFiles(folder, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .Except(tables.Select(e => e.RowName))
            .ToArray();

        Assert.IsEmpty(differing, "Rows that differ from Structures.json: " + string.Join(", ", differing));
        Assert.IsEmpty(undefined, "Rows that Structures.json does not define: " + string.Join(", ", undefined));
    }
}
