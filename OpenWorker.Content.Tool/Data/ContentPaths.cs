namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Folders the content tool reads: client tables and the unpacked GUI icons.
/// </summary>
internal static class ContentPaths
{
    public const string ResVariable = "OPENWORKER_TEST_RES";
    public const string GuiVariable = "OPENWORKER_CONTENT_GUI";

    public static string? ResFolder =>
        FirstExisting(Environment.GetEnvironmentVariable(ResVariable));

    public static string? GuiFolder =>
        FirstExisting(Environment.GetEnvironmentVariable(GuiVariable));

    private static string? FirstExisting(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Directory.Exists(path) ? path : null;
    }
}
