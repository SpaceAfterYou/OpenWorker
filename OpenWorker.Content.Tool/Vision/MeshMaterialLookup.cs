using System.Xml.Linq;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// Reads the first diffuse map from a Vision <c>materials.xml</c> next to a <c>.vmesh</c>.
/// </summary>
internal static class MeshMaterialLookup
{
    public static string? FindDiffusePath(string meshPath, string datasRoot)
    {
        var materialsPath = meshPath + "_data";
        materialsPath = Path.Combine(materialsPath, "materials.xml");
        if (!File.Exists(materialsPath))
        {
            return null;
        }

        try
        {
            var document = XDocument.Load(materialsPath);
            foreach (var material in document.Descendants("Material"))
            {
                var diffuse = material.Attribute("diffuse")?.Value;
                if (string.IsNullOrWhiteSpace(diffuse))
                {
                    continue;
                }

                var resolved = ResolveTexturePath(diffuse, Path.GetDirectoryName(materialsPath)!, datasRoot);
                if (resolved is not null)
                {
                    return resolved;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public static string? ResolveTexturePath(string relative, string materialDirectory, string datasRoot)
    {
        var normalized = relative.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        var besideMaterial = Path.GetFullPath(Path.Combine(materialDirectory, normalized));
        if (File.Exists(besideMaterial))
        {
            return besideMaterial;
        }

        var trimmed = normalized.TrimStart(Path.DirectorySeparatorChar);
        while (trimmed.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            trimmed = trimmed[(3)..];
        }

        var underWorld = Path.GetFullPath(Path.Combine(datasRoot, "World", trimmed));
        if (File.Exists(underWorld))
        {
            return underWorld;
        }

        var underDatas = Path.GetFullPath(Path.Combine(datasRoot, trimmed));
        return File.Exists(underDatas) ? underDatas : null;
    }
}
