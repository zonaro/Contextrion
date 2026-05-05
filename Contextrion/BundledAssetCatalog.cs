using System.Reflection;

namespace Contextrion;

internal static class BundledAssetCatalog
{
    private const string ClipboardIconResourceName = "Contextrion.Assets.clipboard.ico";
    private const string FolderizeIconResourceName = "Contextrion.Assets.folderize.ico";
    private const string BuiltInPrefix = "Contextrion.FolderAssets.BuiltIn.";
    private static readonly string[] BuiltInFolders = ["Win11set", "Win10set", "Win7_8set"];
    private static readonly Lazy<Dictionary<string, string>> BuiltInResourceMap = new(CreateBuiltInResourceMap);

    public static Stream OpenClipboardIconStream() => OpenRequiredResourceStream(ClipboardIconResourceName);

    public static Stream OpenFolderizeIconStream() => OpenRequiredResourceStream(FolderizeIconResourceName);

    public static IEnumerable<(string FolderName, string ResourceName, string Label)> EnumerateBuiltInIcons()
    {
        foreach (var pair in BuiltInResourceMap.Value.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!TryParseBuiltInResourceKey(pair.Key, out var folderName, out var label))
            {
                continue;
            }

            yield return (folderName, pair.Value, label);
        }
    }

    public static bool IsBundledIconPath(string path)
    {
        return path.StartsWith("embedded://", StringComparison.OrdinalIgnoreCase);
    }

    public static Stream OpenBundledIconStream(string path)
    {
        var resourceName = GetResourceNameFromPath(path);
        return OpenRequiredResourceStream(resourceName);
    }

    public static string GetBundledIconPath(string resourceName)
    {
        return $"embedded://{resourceName}";
    }

    public static string EnsureClipboardIconFile(string rootDirectory)
    {
        return EnsureResourceFile(rootDirectory, "Assets", "clipboard.ico", ClipboardIconResourceName);
    }

    public static string EnsureFolderizeIconFile(string rootDirectory)
    {
        return EnsureResourceFile(rootDirectory, "Assets", "folderize.ico", FolderizeIconResourceName);
    }

    public static string EnsureIconFile(FolderIconEntry entry)
    {
        if (!IsBundledIconPath(entry.ResourcePath))
        {
            return entry.ResourcePath;
        }

        var resourceName = GetResourceNameFromPath(entry.ResourcePath);
        var relativeName = resourceName.StartsWith(BuiltInPrefix, StringComparison.OrdinalIgnoreCase)
            ? resourceName[BuiltInPrefix.Length..]
            : resourceName;

        var parts = relativeName.Split('.');
        var folderName = parts.Length > 0 ? parts[0] : "BuiltIn";
        var fileName = parts.Length > 1
            ? $"{string.Join('.', parts.Skip(1).Take(parts.Length - 2))}.{parts[^1]}"
            : $"{resourceName.GetHashCode():x8}.ico";

        return EnsureResourceFile(AppPaths.BuiltInCacheDirectory, folderName, fileName, resourceName);
    }

    private static Dictionary<string, string> CreateBuiltInResourceMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var resourceName in GetAssembly().GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(BuiltInPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryCreateBuiltInResourceKey(resourceName, out var key))
            {
                continue;
            }

            map[key] = resourceName;
        }

        return map;
    }

    private static bool TryCreateBuiltInResourceKey(string resourceName, out string key)
    {
        key = string.Empty;
        var relativeName = resourceName[BuiltInPrefix.Length..];
        foreach (var folderName in BuiltInFolders)
        {
            var folderPrefix = $"{folderName}.";
            if (!relativeName.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = relativeName[folderPrefix.Length..];
            if (!fileName.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var label = fileName[..^4];
            key = $"{folderName}|{label}";
            return true;
        }

        return false;
    }

    private static bool TryParseBuiltInResourceKey(string key, out string folderName, out string label)
    {
        folderName = string.Empty;
        label = string.Empty;

        var separatorIndex = key.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex >= key.Length - 1)
        {
            return false;
        }

        folderName = key[..separatorIndex];
        label = key[(separatorIndex + 1)..];
        return true;
    }

    private static string EnsureResourceFile(string rootDirectory, string subDirectory, string fileName, string resourceName)
    {
        var directory = Path.Combine(rootDirectory, subDirectory);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        using var input = OpenRequiredResourceStream(resourceName);
        using var output = File.Create(fullPath);
        input.CopyTo(output);
        return fullPath;
    }

    private static string GetResourceNameFromPath(string path)
    {
        return path["embedded://".Length..];
    }

    private static Stream OpenRequiredResourceStream(string resourceName)
    {
        return GetAssembly().GetManifestResourceStream(resourceName)
               ?? throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
    }

    private static Assembly GetAssembly()
    {
        return typeof(BundledAssetCatalog).Assembly;
    }
}
