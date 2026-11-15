using System.Collections.ObjectModel;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Tables shipped with the client, for the tests that check the code against them:
/// set OPENWORKER_TEST_RES to the folder with the extracted *.res files.
/// </summary>
internal static class TestTables
{
    private static string Folder => Environment.GetEnvironmentVariable("OPENWORKER_TEST_RES") ?? string.Empty;

    public static ReadOnlyCollection<T> Load<T>(string table) where T : struct
    {
        if (string.IsNullOrEmpty(Folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_RES is not set.");
        }

        return Read<T>(Path.Join(Folder, $"{table}.res"));
    }

    /// <summary>
    /// A table only the server has: set OPENWORKER_TEST_SERVER_RES to the folder with them.
    /// </summary>
    public static ReadOnlyCollection<T> LoadServer<T>(string table) where T : struct
    {
        var folder = Environment.GetEnvironmentVariable("OPENWORKER_TEST_SERVER_RES");

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive("OPENWORKER_TEST_SERVER_RES is not set.");
        }

        return Read<T>(Path.Join(folder, $"{table}.res"));
    }

    private static ReadOnlyCollection<T> Read<T>(string path) where T : struct
    {
        using var reader = new BinaryReader(File.OpenRead(path));

        var rows = new T[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = (T)Activator.CreateInstance(typeof(T), reader)!;
        }

        return new ReadOnlyCollection<T>(rows);
    }
}
