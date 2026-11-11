namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Folders the content tool reads: client tables, unpacked GUI icons, map batch files,
/// navigation meshes and the unpacked World tree (scenes and static meshes).
/// </summary>
internal static class ContentPaths
{
    public const string ResVariable = "OPENWORKER_TEST_RES";
    public const string GuiVariable = "OPENWORKER_CONTENT_GUI";
    public const string BatchVariable = "OPENWORKER_TEST_BATCH";
    public const string NavMeshVariable = "OPENWORKER_TEST_NAVMESH";
    public const string WorldVariable = "OPENWORKER_CONTENT_WORLD";

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

    /// <summary>
    /// Folder of Havok <c>*.hkt</c> navigation meshes. Falls back to a <c>client_navmesh</c> sibling of the res folder.
    /// </summary>
    public static string? NavMeshFolder
    {
        get
        {
            var explicitFolder = FirstExisting(Environment.GetEnvironmentVariable(NavMeshVariable));
            if (explicitFolder is not null)
            {
                return explicitFolder;
            }

            var res = ResFolder;
            if (res is null)
            {
                return null;
            }

            var sibling = Path.GetFullPath(Path.Combine(res, "..", "client_navmesh"));
            return FirstExisting(sibling);
        }
    }

    /// <summary>
    /// Unpacked client data root that contains <c>World/Scenes</c> and <c>World/Meshes</c>
    /// (or the <c>World</c> folder itself). Used for Vision scene geometry.
    /// </summary>
    public static string? WorldFolder
    {
        get
        {
            var explicitFolder = FirstExisting(Environment.GetEnvironmentVariable(WorldVariable));
            if (explicitFolder is null)
            {
                return null;
            }

            var world = Path.Combine(explicitFolder, "World");
            if (Directory.Exists(Path.Combine(world, "Scenes")))
            {
                return Path.GetFullPath(world);
            }

            if (Directory.Exists(Path.Combine(explicitFolder, "Scenes")))
            {
                return Path.GetFullPath(explicitFolder);
            }

            return null;
        }
    }

    public static string? ScenesFolder
    {
        get
        {
            var world = WorldFolder;
            return world is null ? null : Path.Combine(world, "Scenes");
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
