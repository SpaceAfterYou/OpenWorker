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

        using var reader = new BinaryReader(File.OpenRead(Path.Join(Folder, $"{table}.res")));

        var rows = new T[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = (T)Activator.CreateInstance(typeof(T), reader)!;
        }

        return new ReadOnlyCollection<T>(rows);
    }
}
