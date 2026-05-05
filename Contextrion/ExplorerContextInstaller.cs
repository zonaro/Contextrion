using Microsoft.Win32;

namespace Contextrion;

internal static class ExplorerContextInstaller
{
    private static string RootMenuText => Translate("titles.app-name", "Contextrion");
    private static string PasteMenuText => Translate("titles.paste-into-file", "Paste Into File");
    private static string CustomizeMenuText => Translate("titles.customize-folder", "Customize Folder");
    private static string RestoreMenuText => Translate("titles.restore-default", "Restore Default");
    private static string NewTimestampFolderMenuText => Translate("titles.new-timestamp-folder", "New Timestamp Folder");
    private static string CleanMetadataMenuText => Translate("menu.clean-metadata", "Clean metadata");
    private const string DirectBackgroundPasteRegistryPath = @"Software\Classes\Directory\Background\shell\Contextrion";
    private const string DirectDirectoryPasteRegistryPath = @"Software\Classes\Directory\shell\Contextrion";
    private const string FolderBackgroundRegistryPath = @"Software\Classes\Directory\Background\shell\Contextrion.More";
    private const string DirectoryRegistryPath = @"Software\Classes\Directory\shell\Contextrion.More";
    private const string FileRegistryPath = @"Software\Classes\*\shell\Contextrion.Tools";
    private const string ImageFileRegistryPath = @"Software\Classes\SystemFileAssociations\image\shell\Contextrion.Tools";
    private const string CssFileRegistryPath = @"Software\Classes\.css\shell\Contextrion.Tools";
    private const string JsFileRegistryPath = @"Software\Classes\.js\shell\Contextrion.Tools";
    private const string JpgCleanMetadataRegistryPath = @"Software\Classes\.jpg\shell\Contextrion.CleanMetadata";
    private const string JpegCleanMetadataRegistryPath = @"Software\Classes\.jpeg\shell\Contextrion.CleanMetadata";
    private const string PngCleanMetadataRegistryPath = @"Software\Classes\.png\shell\Contextrion.CleanMetadata";
    private const string CustomFolderClassRegistryPath = @"Software\Classes\Contextrion.CustomFolder";

    public static string InstallDirectory => AppPaths.InstallDirectory;

    public static bool IsInstalled()
    {
        var executablePath = GetInstalledExecutablePath();
        return File.Exists(executablePath) &&
               Registry.CurrentUser.OpenSubKey(FolderBackgroundRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(DirectoryRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(FileRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(ImageFileRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(CssFileRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(JsFileRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(JpgCleanMetadataRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(JpegCleanMetadataRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(PngCleanMetadataRegistryPath) is not null;
    }

    public static void Install()
    {
        EnsureAdministrator();

        TerminateOtherInstances();
        CopyApplicationFiles();
        RemoveLegacyRegistryEntries();
        CreateRegistryEntries();
        FolderCustomizationService.RefreshShell();
    }

    public static void Uninstall()
    {
        EnsureAdministrator();

        RemoveLegacyRegistryEntries();
        Registry.CurrentUser.DeleteSubKeyTree(DirectBackgroundPasteRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(DirectDirectoryPasteRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(FolderBackgroundRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(DirectoryRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(FileRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(ImageFileRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(CssFileRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(JsFileRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(JpgCleanMetadataRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(JpegCleanMetadataRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(PngCleanMetadataRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(CustomFolderClassRegistryPath, throwOnMissingSubKey: false);
        FolderCustomizationService.RefreshShell();
    }

    public static string GetInstalledExecutablePath()
    {
        return Path.Combine(InstallDirectory, Path.GetFileName(Program.ExecutablePath));
    }

    public static string GetInstalledIconPath()
    {
        return BundledAssetCatalog.EnsureClipboardIconFile(InstallDirectory);
    }

    public static string GetInstalledFolderIconPath()
    {
        return BundledAssetCatalog.EnsureFolderizeIconFile(InstallDirectory);
    }

    private static void CopyApplicationFiles()
    {
        var sourceDirectory = AppContext.BaseDirectory;
        Directory.CreateDirectory(InstallDirectory);

        foreach (var sourceFile in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var destinationFile = Path.Combine(InstallDirectory, relativePath);

            if (string.Equals(
                Path.GetFullPath(sourceFile),
                Path.GetFullPath(destinationFile),
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destinationDirectory = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceFile, destinationFile, overwrite: true);
        }

        AppPaths.EnsureDirectory(Path.Combine(InstallDirectory, "FolderAssets", "CustomPacks"));
        AppPaths.EnsureDirectory(Path.Combine(InstallDirectory, "FolderAssets", "UserIcons"));
        _ = GetInstalledIconPath();
        _ = GetInstalledFolderIconPath();
    }

    private static void TerminateOtherInstances()
    {
        var currentProcess = Process.GetCurrentProcess();
        var currentProcessId = currentProcess.Id;
        var processName = Path.GetFileNameWithoutExtension(Program.ExecutablePath);

        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                if (process.Id == currentProcessId)
                {
                    continue;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static void CreateRegistryEntries()
    {
        var executablePath = GetInstalledExecutablePath();
        var clipboardIconPath = GetInstalledIconPath();
        var folderIconPath = GetInstalledFolderIconPath();

        CreateDirectPasteEntry(DirectBackgroundPasteRegistryPath, clipboardIconPath, $"\"{executablePath}\" --paste \"%V\"");
        CreateDirectPasteEntry(DirectDirectoryPasteRegistryPath, clipboardIconPath, $"\"{executablePath}\" --paste \"%1\"");
        CreateBackgroundMenuEntry(executablePath, clipboardIconPath);
        CreateDirectoryMenuEntry(executablePath, clipboardIconPath, folderIconPath);
        CreateGeneralFileMenuEntry(executablePath, clipboardIconPath);
        CreateImageFileMenuEntry(executablePath, clipboardIconPath);
        CreateCleanMetadataMenuEntries(executablePath, clipboardIconPath);
        CreateMinifyFileMenuEntries(executablePath, clipboardIconPath);
        CreateCustomFolderClass();
    }

    private static void CreateDirectPasteEntry(string registryPath, string iconPath, string commandValue)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(registryPath);
        menuKey?.SetValue(string.Empty, PasteMenuText);
        menuKey?.SetValue("Icon", iconPath);

        using var commandKey = Registry.CurrentUser.CreateSubKey($@"{registryPath}\command");
        commandKey?.SetValue(string.Empty, commandValue);
    }

    private static void CreateBackgroundMenuEntry(string executablePath, string clipboardIconPath)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(FolderBackgroundRegistryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", GetInstalledFolderIconPath());
        menuKey?.SetValue("SubCommands", string.Empty);

        CreateSubMenuCommand($@"{FolderBackgroundRegistryPath}\shell\0000", CustomizeMenuText, GetInstalledFolderIconPath(), $"\"{executablePath}\" --pick --folder \"%V\"");
        CreateSubMenuCommand($@"{FolderBackgroundRegistryPath}\shell\0001", RestoreMenuText, GetInstalledFolderIconPath(), $"\"{executablePath}\" --restore-folder --folder \"%V\"");
        CreateSubMenuCommand($@"{FolderBackgroundRegistryPath}\shell\0002", NewTimestampFolderMenuText, GetInstalledFolderIconPath(), $"\"{executablePath}\" --new-timestamp-folder --folder \"%V\"");
    }

    private static void CreateDirectoryMenuEntry(string executablePath, string clipboardIconPath, string folderIconPath)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(DirectoryRegistryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", folderIconPath);
        menuKey?.SetValue("SubCommands", string.Empty);
        menuKey?.SetValue("MultiSelectModel", "Player");

        CreateSubMenuCommand($@"{DirectoryRegistryPath}\shell\0000", CustomizeMenuText, folderIconPath, $"\"{executablePath}\" --pick --folder \"%1\"");
        CreateSubMenuCommand($@"{DirectoryRegistryPath}\shell\0001", RestoreMenuText, folderIconPath, $"\"{executablePath}\" --restore-folder --folder \"%1\"");
        CreateSubMenuCommand($@"{DirectoryRegistryPath}\shell\0002", NewTimestampFolderMenuText, folderIconPath, $"\"{executablePath}\" --new-timestamp-folder --folder \"%1\"");
        CreateCommands(
            DirectoryRegistryPath,
            executablePath,
            clipboardIconPath,
            startIndex: 100,
            separatorBeforeFirst: true,
            entries:
            [
                (Translate("menu.rename-friendly-url", "Rename to Friendly URL"), "--friendly-name"),
                (Translate("menu.copy-data-url", "Copy as Data URL"), "--tobase64"),
                (Translate("menu.copy-file-path", "Copy File Path"), "--copy-path"),
                (Translate("menu.bulk-rename", "Bulk Rename and Enumerate"), "--enum"),
                (Translate("menu.combine-vertical", "Combine Images Vertically"), "--combine-vertical"),
                (Translate("menu.combine-horizontal", "Combine Images Horizontally"), "--combine-horizontal"),
                (Translate("menu.clean-empty-directories", "Clean Empty Directories"), "--clean-empty"),
                (Translate("menu.copy-file-content", "Copy File Content"), "--copy-content"),
                (Translate("menu.grayscale", "Convert Image to Grayscale"), "--grayscale"),
                (Translate("menu.apply-watermark", "Apply Watermark"), "--watermark"),
                (Translate("menu.crop-images", "Crop Images"), "--crop"),
                (Translate("menu.crop-circle", "Crop Images to Circle"), "--circle"),
                (Translate("menu.resize-images", "Resize Images"), "--resize"),
                (Translate("menu.invert-image-colors", "Invert Image Colors"), "--invert-color"),
                (Translate("menu.optimize-images", "Optimize Images for Web"), "--optimize")
            ]);
    }

    private static void CreateGeneralFileMenuEntry(string executablePath, string iconPath)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(FileRegistryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", iconPath);
        menuKey?.SetValue("SubCommands", string.Empty);
        menuKey?.SetValue("MultiSelectModel", "Player");

        CreateCommands(
            FileRegistryPath,
            executablePath,
            iconPath,
            startIndex: 0,
            separatorBeforeFirst: false,
            entries:
            [
                (Translate("menu.rename-friendly-url", "Rename to Friendly URL"), "--friendly-name"),
                (Translate("menu.copy-data-url", "Copy as Data URL"), "--tobase64"),
                (Translate("menu.copy-file-path", "Copy File Path"), "--copy-path"),
                (Translate("menu.bulk-rename", "Bulk Rename and Enumerate"), "--enum"),
                (Translate("menu.copy-file-content", "Copy File Content"), "--copy-content")
            ]);
    }

    private static void CreateImageFileMenuEntry(string executablePath, string iconPath)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(ImageFileRegistryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", iconPath);
        menuKey?.SetValue("SubCommands", string.Empty);
        menuKey?.SetValue("MultiSelectModel", "Player");

        CreateCommands(
            ImageFileRegistryPath,
            executablePath,
            iconPath,
            startIndex: 0,
            separatorBeforeFirst: false,
            entries:
            [
                (Translate("menu.combine-vertical", "Combine Images Vertically"), "--combine-vertical"),
                (Translate("menu.combine-horizontal", "Combine Images Horizontally"), "--combine-horizontal"),
                (Translate("menu.grayscale", "Convert Image to Grayscale"), "--grayscale"),
                (Translate("menu.apply-watermark", "Apply Watermark"), "--watermark"),
                (Translate("menu.crop-images", "Crop Images"), "--crop"),
                (Translate("menu.crop-circle", "Crop Images to Circle"), "--circle"),
                (Translate("menu.resize-images", "Resize Images"), "--resize"),
                (Translate("menu.invert-image-colors", "Invert Image Colors"), "--invert-color"),
                (Translate("menu.optimize-images", "Optimize Images for Web"), "--optimize")
            ]);
    }

    private static void CreateMinifyFileMenuEntries(string executablePath, string iconPath)
    {
        var menuText = Translate("menu.minify-js-css", "Minify JS or CSS");
        CreateSingleCommandMenu(CssFileRegistryPath, executablePath, iconPath, menuText, "--minify");
        CreateSingleCommandMenu(JsFileRegistryPath, executablePath, iconPath, menuText, "--minify");
    }

    private static void CreateCleanMetadataMenuEntries(string executablePath, string iconPath)
    {
        CreateDirectFileCommandMenu(JpgCleanMetadataRegistryPath, executablePath, iconPath, CleanMetadataMenuText, "--clean-metadata");
        CreateDirectFileCommandMenu(JpegCleanMetadataRegistryPath, executablePath, iconPath, CleanMetadataMenuText, "--clean-metadata");
        CreateDirectFileCommandMenu(PngCleanMetadataRegistryPath, executablePath, iconPath, CleanMetadataMenuText, "--clean-metadata");
    }

    private static void CreateSingleCommandMenu(string registryPath, string executablePath, string iconPath, string menuText, string argument)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(registryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", iconPath);
        menuKey?.SetValue("SubCommands", string.Empty);
        menuKey?.SetValue("MultiSelectModel", "Player");

        CreateCommands(
            registryPath,
            executablePath,
            iconPath,
            startIndex: 0,
            separatorBeforeFirst: false,
            entries:
            [
                (menuText, argument)
            ]);
    }

    private static void CreateDirectFileCommandMenu(string registryPath, string executablePath, string iconPath, string menuText, string argument)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(registryPath);
        menuKey?.SetValue(string.Empty, menuText);
        menuKey?.SetValue("Icon", iconPath);
        menuKey?.SetValue("MultiSelectModel", "Player");

        using var commandKey = Registry.CurrentUser.CreateSubKey($@"{registryPath}\command");
        commandKey?.SetValue(string.Empty, $"\"{executablePath}\" {argument} %*");
    }

    private static void CreateSubMenuCommand(string registryPath, string menuText, string iconPath, string commandValue, bool separatorBefore = false)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(registryPath);
        menuKey?.SetValue("MUIVerb", menuText);
        menuKey?.SetValue("Icon", iconPath);
        if (separatorBefore)
        {
            menuKey?.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord);
        }

        using var commandKey = Registry.CurrentUser.CreateSubKey($@"{registryPath}\command");
        commandKey?.SetValue(string.Empty, commandValue);
    }

    private static void CreateCustomFolderClass()
    {
        using var key = Registry.CurrentUser.CreateSubKey(CustomFolderClassRegistryPath);
        key?.SetValue(string.Empty, Translate("menu.custom-folder-class", "Contextrion Custom Folder"));
        key?.SetValue("CanUseForDirectory", string.Empty);
    }

    private static void RemoveLegacyRegistryEntries()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\Background\shell\ClipboardFiles", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\ClipboardFiles", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\Background\shell\ClipboardFiles.More", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\ClipboardFiles.More", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\*\shell\ClipboardFiles.Tools", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\ClipboardFiles.CustomFolder", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\Background\shell\Contextrion", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\Contextrion", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\Background\shell\Contextrion.More", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Directory\shell\Contextrion.More", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\*\shell\Contextrion.Tools", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\SystemFileAssociations\image\shell\Contextrion.Tools", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.css\shell\Contextrion.Tools", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.js\shell\Contextrion.Tools", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.jpg\shell\Contextrion.CleanMetadata", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.jpeg\shell\Contextrion.CleanMetadata", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.png\shell\Contextrion.CleanMetadata", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Contextrion.CustomFolder", throwOnMissingSubKey: false);
    }

    private static void CreateCommands(string baseRegistryPath, string executablePath, string iconPath, int startIndex, bool separatorBeforeFirst, IReadOnlyList<(string Text, string Argument)> entries)
    {
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var registryPath = $@"{baseRegistryPath}\shell\{(startIndex + index).ToString("0000")}";
            CreateSubMenuCommand(
                registryPath,
                entry.Text,
                iconPath,
                $"\"{executablePath}\" {entry.Argument} %*",
                separatorBefore: separatorBeforeFirst && index == 0);
        }
    }

    private static void EnsureAdministrator()
    {
        if (!Program.IsAdministrator())
        {
            throw new InvalidOperationException(
                Translate(
                    "installer.run-as-admin",
                    "Run the application as administrator to install or uninstall."));
        }
    }
}
