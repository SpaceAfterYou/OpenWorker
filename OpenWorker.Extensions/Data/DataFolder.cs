namespace OpenWorker.Extensions.Data;

/// <summary>
/// A folder of game data a server reads, by its path relative to the folder the server runs in.
/// </summary>
/// <param name="Path">Where the server reads it.</param>
/// <param name="Holds">What it holds, in words, for the line that says it is missing.</param>
/// <param name="IsFilledIn">Whether the folder under a root holds files of its kind: an empty folder counts as missing.</param>
public sealed record DataFolder(string Path, string Holds, Func<string, bool> IsFilledIn)
{
    private static readonly EnumerationOptions AnyCase = new() { MatchCasing = MatchCasing.CaseInsensitive };

    /// <summary>
    /// A folder that is filled when it holds at least one file of the pattern.
    /// </summary>
    public static DataFolder Of(string path, string pattern, string holds)
    {
        return new DataFolder(path, holds, root => Files(System.IO.Path.Join(root, path), pattern).Any());
    }

    /// <summary>
    /// The files of the pattern in a folder, none when there is no such folder.
    /// </summary>
    public static IEnumerable<string> Files(string folder, string pattern)
    {
        return Directory.Exists(folder) ? Directory.EnumerateFiles(folder, pattern, AnyCase) : [];
    }
}
