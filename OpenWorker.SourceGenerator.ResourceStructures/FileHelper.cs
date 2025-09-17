namespace OpenWorker.SourceGenerator.ResourceStructures;

internal static class FileHelper
{
    private const string ModelNamespace = "OpenWorker.Domain.Persistent.Game.Resource";
    private const string SeedNamespace = "OpenWorker.UpdateContent.Res.Rows";

    internal static async ValueTask WriteRes(string root,
        string tableName,
        string modelName,
        string className,
        IReadOnlyList<Type> types,
        CancellationToken ct)
    {
        var dir = Path.Join(root, "OpenWorker.Res", "Rows");
        if (Directory.Exists(dir) is false)
        {
            Directory.CreateDirectory(dir);
        }

        var content =
            await ClassHelper.GetResContent(tableName, modelName, className, ModelNamespace, SeedNamespace, types);
        await File.WriteAllTextAsync(Path.Join(dir, $"{className}.cs"), content, ct);
    }

    internal static async ValueTask WriteSeed(string root,
        string tableName,
        string modelName,
        string className,
        IReadOnlyList<Type> types,
        CancellationToken ct)
    {
        var dir = Path.Join(root, "OpenWorker.UpdateContent", "Res", "Rows");
        if (Directory.Exists(dir) is false)
        {
            Directory.CreateDirectory(dir);
        }

        var content =
            await ClassHelper.GetClassContent(tableName, modelName, className, ModelNamespace, SeedNamespace, types);
        await File.WriteAllTextAsync(Path.Join(dir, $"{className}.cs"), content, ct);
    }

    internal static async ValueTask WriteModel(string root,
        string tableName,
        string className,
        IReadOnlyList<Type> types,
        CancellationToken ct)
    {
        var dir = Path.Join(root, "OpenWorker.Domain", "Persistent", "Game", "Resource");
        if (Directory.Exists(dir) is false)
        {
            Directory.CreateDirectory(dir);
        }

        var content = await ClassHelper.GetModelContent(tableName, className, ModelNamespace, types);
        await File.WriteAllTextAsync(Path.Join(dir, $"{className}.cs"), content, ct);
    }
}