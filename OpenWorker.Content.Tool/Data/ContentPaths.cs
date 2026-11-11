namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Folders the content tool reads: client tables, unpacked GUI icons and map batch files.
/// </summary>
internal static class ContentPaths
{
    public const string ResVariable = "OPENWORKER_TEST_RES";
    public const string GuiVariable = "OPENWORKER_CONTENT_GUI";
    public const string BatchVariable = "OPENWORKER_TEST_BATCH";

    public static string? ResFolder =>
        FirstExisting(Environment.GetEnvironmentVariable(ResVariable));

    public static string? GuiFolder =>
        FirstExisting(Environment.GetEnvironmentVariable(GuiVariable));

    /// <summary>
    /// Folder of <c>*.vbatch</c> files. Falls back to a <c>client_batch</c> sibling of the res folder.
    /// </summary>
    public static string? BatchFolder
    {
        get
        {
            var explicitFolder = FirstExisting(Environment.GetEnvironmentVariable(BatchVariable));
            if (explicitFolder is not null)
            {
                return explicitFolder;
            }

            var res = ResFolder;
            if (res is null)
            {
                return null;
            }

            var sibling = Path.GetFullPath(Path.Combine(res, "..", "client_batch"));
            return FirstExisting(sibling);
        }
    }

    private static string? FirstExisting(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Directory.Exists(path) ? path : null;
    }
}
