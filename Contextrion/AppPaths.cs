namespace Contextrion;

internal static class AppPaths
{
    public const string InstallFolderName = "Contextrion";
    public const string CustomFolderClass = "Contextrion.CustomFolder";
    private static readonly Lazy<string> InitializedLocalDataDirectory = new(InitializeLocalDataDirectory);

    public static string LocalDataDirectory =>
        InitializedLocalDataDirectory.Value;

    public static string AssetsDirectory => Path.Combine(AppContext.BaseDirectory, "Assets");

    public static string FolderAssetsDirectory => Path.Combine(AppContext.BaseDirectory, "FolderAssets");

    public static string BuiltInIconsDirectory => Path.Combine(FolderAssetsDirectory, "BuiltIn");

    public static string UserDataFolderAssetsDirectory => Path.Combine(LocalDataDirectory, "FolderAssets");

    public static string CustomPacksDirectory => Path.Combine(UserDataFolderAssetsDirectory, "CustomPacks");

    public static string UserIconsDirectory => Path.Combine(UserDataFolderAssetsDirectory, "UserIcons");

    public static string BuiltInCacheDirectory => Path.Combine(LocalDataDirectory, "Cache", "BuiltInIcons");

    public static string ReadmePath => Path.Combine(AppContext.BaseDirectory, "README.md");

    public static string ClipboardIconPath => Path.Combine(AssetsDirectory, "clipboard.ico");

    public static string FolderizeIconPath => Path.Combine(AssetsDirectory, "folderize.ico");

    public static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private static string InitializeLocalDataDirectory()
    {
        var localDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), InstallFolderName);
        Directory.CreateDirectory(localDataDirectory);

        var legacyFolderAssetsDirectory = Path.Combine(AppContext.BaseDirectory, "FolderAssets");
        MigrateDirectory(
            Path.Combine(legacyFolderAssetsDirectory, "UserIcons"),
            Path.Combine(localDataDirectory, "FolderAssets", "UserIcons"));
        MigrateDirectory(
            Path.Combine(legacyFolderAssetsDirectory, "CustomPacks"),
            Path.Combine(localDataDirectory, "FolderAssets", "CustomPacks"));

        return localDataDirectory;
    }

    private static void MigrateDirectory(string sourceDirectory, string destinationDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        if (string.Equals(
                Path.GetFullPath(sourceDirectory),
                Path.GetFullPath(destinationDirectory),
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var sourcePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            var destinationParent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationParent))
            {
                Directory.CreateDirectory(destinationParent);
            }

            if (!File.Exists(destinationPath))
            {
                File.Copy(sourcePath, destinationPath);
            }
        }
    }
}
