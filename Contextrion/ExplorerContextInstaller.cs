using Microsoft.Win32;

namespace Contextrion;

internal static class ExplorerContextInstaller
{
    private static string RootMenuText => Translate("titles.app-name", "Contextrion");
    private static string PasteMenuText => Translate("titles.paste-into-file", "Paste Into File");
    private static string FolderGroupMenuText => Translate("menu.group.folder", "Folder");
    private static string FilesGroupMenuText => Translate("menu.group.files", "Files");
    private static string ImagesGroupMenuText => Translate("menu.group.images", "Images");
    private static string TextGroupMenuText => Translate("menu.group.text", "Text");
    private static string CustomizeMenuText => Translate("titles.customize-folder", "Customize Folder");
    private static string RestoreMenuText => Translate("titles.restore-default", "Restore Default");
    private static string NewTimestampFolderMenuText => Translate("titles.new-timestamp-folder", "New Timestamp Folder");
    private static string CleanMetadataMenuText => Translate("menu.clean-metadata", "Clean metadata");
    private const string DirectBackgroundPasteRegistryPath = @"Software\Classes\Directory\Background\shell\Contextrion";
    private const string DirectDirectoryPasteRegistryPath = @"Software\Classes\Directory\shell\Contextrion";
    private const string FolderBackgroundRegistryPath = @"Software\Classes\Directory\Background\shell\Contextrion.More";
    private const string DirectoryRegistryPath = @"Software\Classes\Directory\shell\Contextrion.More";
    private const string FileRegistryPath = @"Software\Classes\*\shell\Contextrion.Tools";
    private const string CustomFolderClassRegistryPath = @"Software\Classes\Contextrion.CustomFolder";
    private const string ImageFilesAppliesTo = "System.Kind:=picture";
    private const string TextFilesAppliesTo = "System.FileExtension:=\".js\" OR System.FileExtension:=\".css\"";
    private const string MetadataFilesAppliesTo = "System.FileExtension:=\".jpg\" OR System.FileExtension:=\".jpeg\" OR System.FileExtension:=\".png\"";
    private const string ExplorerItemTargetExpression = "\"%1\"";
    private const string ExplorerBackgroundTargetExpression = "\"%V\"";

    public static bool IsInstalled()
    {
        return Registry.CurrentUser.OpenSubKey(FolderBackgroundRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(DirectoryRegistryPath) is not null &&
               Registry.CurrentUser.OpenSubKey(FileRegistryPath) is not null;
    }

    public static void Install()
    {
        EnsureContextMenuAssets();
        RemoveLegacyRegistryEntries();
        CreateRegistryEntries();
        FolderCustomizationService.RefreshShell();
    }

    public static void Uninstall()
    {
        RemoveLegacyRegistryEntries();
        Registry.CurrentUser.DeleteSubKeyTree(DirectBackgroundPasteRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(DirectDirectoryPasteRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(FolderBackgroundRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(DirectoryRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(FileRegistryPath, throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree(CustomFolderClassRegistryPath, throwOnMissingSubKey: false);
        FolderCustomizationService.RefreshShell();
    }

    public static string GetContextMenuExecutablePath()
    {
        return Path.GetFullPath(Program.ExecutablePath);
    }

    public static string GetContextMenuIconPath()
    {
        return BundledAssetCatalog.EnsureClipboardIconFile(AppPaths.LocalDataDirectory);
    }

    public static string GetContextMenuFolderIconPath()
    {
        return BundledAssetCatalog.EnsureFolderizeIconFile(AppPaths.LocalDataDirectory);
    }

    private static void EnsureContextMenuAssets()
    {
        AppPaths.EnsureDirectory(AppPaths.CustomPacksDirectory);
        AppPaths.EnsureDirectory(AppPaths.UserIconsDirectory);
        _ = GetContextMenuIconPath();
        _ = GetContextMenuFolderIconPath();
    }

    private static void CreateRegistryEntries()
    {
        var executablePath = GetContextMenuExecutablePath();
        var clipboardIconPath = GetContextMenuIconPath();
        var folderIconPath = GetContextMenuFolderIconPath();

        CreateDirectPasteEntry(DirectBackgroundPasteRegistryPath, clipboardIconPath, $"\"{executablePath}\" --paste {ExplorerBackgroundTargetExpression}");
        CreateDirectPasteEntry(DirectDirectoryPasteRegistryPath, clipboardIconPath, $"\"{executablePath}\" --paste \"%1\"");
        CreateBackgroundMenuEntry(executablePath, clipboardIconPath, folderIconPath);
        CreateDirectoryMenuEntry(executablePath, clipboardIconPath, folderIconPath);
        CreateFileMenuEntry(executablePath, clipboardIconPath);
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

    private static void CreateBackgroundMenuEntry(string executablePath, string clipboardIconPath, string folderIconPath)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(FolderBackgroundRegistryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", folderIconPath);
        menuKey?.SetValue("SubCommands", string.Empty);

        CreateFolderGroupForBackground(FolderBackgroundRegistryPath, executablePath, clipboardIconPath, folderIconPath);
        CreateFilesGroup(FolderBackgroundRegistryPath, executablePath, clipboardIconPath, startIndex: 100, separatorBefore: true, targetExpression: ExplorerBackgroundTargetExpression);
        CreateImagesGroup(FolderBackgroundRegistryPath, executablePath, clipboardIconPath, startIndex: 200, separatorBefore: true, targetExpression: ExplorerBackgroundTargetExpression);
        CreateTextGroup(FolderBackgroundRegistryPath, executablePath, clipboardIconPath, startIndex: 300, separatorBefore: true, targetExpression: ExplorerBackgroundTargetExpression);
    }

    private static void CreateDirectoryMenuEntry(string executablePath, string clipboardIconPath, string folderIconPath)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(DirectoryRegistryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", folderIconPath);
        menuKey?.SetValue("SubCommands", string.Empty);
        menuKey?.SetValue("MultiSelectModel", "Player");

        CreateFolderGroupForDirectory(DirectoryRegistryPath, executablePath, clipboardIconPath, folderIconPath);
        CreateFilesGroup(DirectoryRegistryPath, executablePath, clipboardIconPath, startIndex: 100, separatorBefore: true, targetExpression: ExplorerItemTargetExpression);
        CreateImagesGroup(DirectoryRegistryPath, executablePath, clipboardIconPath, startIndex: 200, separatorBefore: true, targetExpression: ExplorerItemTargetExpression);
        CreateTextGroup(DirectoryRegistryPath, executablePath, clipboardIconPath, startIndex: 300, separatorBefore: true, targetExpression: ExplorerItemTargetExpression);
    }

    private static void CreateFileMenuEntry(string executablePath, string iconPath)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(FileRegistryPath);
        menuKey?.SetValue("MUIVerb", RootMenuText);
        menuKey?.SetValue("Icon", iconPath);
        menuKey?.SetValue("SubCommands", string.Empty);
        menuKey?.SetValue("MultiSelectModel", "Player");

        CreateFilesGroup(FileRegistryPath, executablePath, iconPath, startIndex: 0, separatorBefore: false, targetExpression: ExplorerItemTargetExpression);
        CreateImagesGroup(FileRegistryPath, executablePath, iconPath, startIndex: 100, separatorBefore: true, targetExpression: ExplorerItemTargetExpression, appliesTo: ImageFilesAppliesTo);
        CreateTextGroup(FileRegistryPath, executablePath, iconPath, startIndex: 200, separatorBefore: true, targetExpression: ExplorerItemTargetExpression, appliesTo: TextFilesAppliesTo);
    }

    private static void CreateFolderGroupForBackground(string baseRegistryPath, string executablePath, string clipboardIconPath, string folderIconPath)
    {
        var groupPath = $@"{baseRegistryPath}\shell\0000";
        CreateSubMenuGroup(groupPath, FolderGroupMenuText, folderIconPath);
        CreateCommand($@"{groupPath}\shell\0000", CustomizeMenuText, folderIconPath, $"\"{executablePath}\" --pick --folder {ExplorerBackgroundTargetExpression}");
        CreateCommand($@"{groupPath}\shell\0001", RestoreMenuText, folderIconPath, $"\"{executablePath}\" --restore-folder --folder {ExplorerBackgroundTargetExpression}");
        CreateCommand($@"{groupPath}\shell\0002", NewTimestampFolderMenuText, folderIconPath, $"\"{executablePath}\" --new-timestamp-folder --folder {ExplorerBackgroundTargetExpression}");
        CreateCommand($@"{groupPath}\shell\0003", Translate("menu.clean-empty-directories", "Clean Empty Directories"), clipboardIconPath, $"\"{executablePath}\" --clean-empty {ExplorerBackgroundTargetExpression}");
    }

    private static void CreateFolderGroupForDirectory(string baseRegistryPath, string executablePath, string clipboardIconPath, string folderIconPath)
    {
        var groupPath = $@"{baseRegistryPath}\shell\0000";
        CreateSubMenuGroup(groupPath, FolderGroupMenuText, folderIconPath);
        CreateCommand($@"{groupPath}\shell\0000", CustomizeMenuText, folderIconPath, $"\"{executablePath}\" --pick --folder \"%1\"");
        CreateCommand($@"{groupPath}\shell\0001", RestoreMenuText, folderIconPath, $"\"{executablePath}\" --restore-folder --folder \"%1\"");
        CreateCommand($@"{groupPath}\shell\0002", NewTimestampFolderMenuText, folderIconPath, $"\"{executablePath}\" --new-timestamp-folder --folder \"%1\"");
        CreateCommand($@"{groupPath}\shell\0003", Translate("menu.clean-empty-directories", "Clean Empty Directories"), clipboardIconPath, $"\"{executablePath}\" --clean-empty {ExplorerItemTargetExpression}");
    }

    private static void CreateFilesGroup(string baseRegistryPath, string executablePath, string iconPath, int startIndex, bool separatorBefore, string targetExpression)
    {
        var groupPath = $@"{baseRegistryPath}\shell\{startIndex.ToString("0000")}";
        CreateSubMenuGroup(groupPath, FilesGroupMenuText, iconPath, separatorBefore: separatorBefore);
        CreateCommands(
            groupPath,
            executablePath,
            iconPath,
            startIndex: 0,
            targetExpression,
            entries:
            [
                new(Translate("menu.rename-friendly-url", "Rename to Friendly URL"), "--friendly-name"),
                new(Translate("menu.copy-data-url", "Copy as Data URL"), "--tobase64"),
                new(Translate("menu.copy-file-path", "Copy File Path"), "--copy-path"),
                new(Translate("menu.bulk-rename", "Bulk Rename and Enumerate"), "--enum"),
                new(Translate("menu.copy-file-content", "Copy File Content"), "--copy-content")
            ]);
    }

    private static void CreateImagesGroup(string baseRegistryPath, string executablePath, string iconPath, int startIndex, bool separatorBefore, string targetExpression, string? appliesTo = null)
    {
        var groupPath = $@"{baseRegistryPath}\shell\{startIndex.ToString("0000")}";
        CreateSubMenuGroup(groupPath, ImagesGroupMenuText, iconPath, separatorBefore: separatorBefore, appliesTo: appliesTo);
        CreateCommands(
            groupPath,
            executablePath,
            iconPath,
            startIndex: 0,
            targetExpression,
            entries:
            [
                new(Translate("menu.combine-vertical", "Combine Images Vertically"), "--combine-vertical"),
                new(Translate("menu.combine-horizontal", "Combine Images Horizontally"), "--combine-horizontal"),
                new(Translate("menu.grayscale", "Convert Image to Grayscale"), "--grayscale"),
                new(Translate("menu.apply-watermark", "Apply Watermark"), "--watermark"),
                new(Translate("menu.crop-images", "Crop Images"), "--crop"),
                new(Translate("menu.crop-circle", "Crop Images to Circle"), "--circle"),
                new(Translate("menu.resize-images", "Resize Images"), "--resize"),
                new(Translate("menu.invert-image-colors", "Invert Image Colors"), "--invert-color"),
                new(CleanMetadataMenuText, "--clean-metadata", MetadataFilesAppliesTo),
                new(Translate("menu.optimize-images", "Optimize Images for Web"), "--optimize")
            ]);
    }

    private static void CreateTextGroup(string baseRegistryPath, string executablePath, string iconPath, int startIndex, bool separatorBefore, string targetExpression, string? appliesTo = null)
    {
        var groupPath = $@"{baseRegistryPath}\shell\{startIndex.ToString("0000")}";
        CreateSubMenuGroup(groupPath, TextGroupMenuText, iconPath, separatorBefore: separatorBefore, appliesTo: appliesTo);
        CreateCommands(
            groupPath,
            executablePath,
            iconPath,
            startIndex: 0,
            targetExpression,
            entries:
            [
                new(Translate("menu.minify-js-css", "Minify JS or CSS"), "--minify")
            ]);
    }

    private static void CreateSubMenuGroup(string registryPath, string menuText, string iconPath, bool separatorBefore = false, string? appliesTo = null)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(registryPath);
        menuKey?.SetValue("MUIVerb", menuText);
        menuKey?.SetValue("Icon", iconPath);
        menuKey?.SetValue("SubCommands", string.Empty);
        if (separatorBefore)
        {
            menuKey?.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord);
        }
        if (!string.IsNullOrWhiteSpace(appliesTo))
        {
            menuKey?.SetValue("AppliesTo", appliesTo);
        }
    }

    private static void CreateCommand(string registryPath, string menuText, string iconPath, string commandValue, bool separatorBefore = false, string? appliesTo = null)
    {
        using var menuKey = Registry.CurrentUser.CreateSubKey(registryPath);
        menuKey?.SetValue("MUIVerb", menuText);
        menuKey?.SetValue("Icon", iconPath);
        if (separatorBefore)
        {
            menuKey?.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord);
        }
        if (!string.IsNullOrWhiteSpace(appliesTo))
        {
            menuKey?.SetValue("AppliesTo", appliesTo);
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

    private static void CreateCommands(string baseRegistryPath, string executablePath, string iconPath, int startIndex, string targetExpression, IReadOnlyList<MenuCommandEntry> entries)
    {
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var registryPath = $@"{baseRegistryPath}\shell\{(startIndex + index).ToString("0000")}";
            CreateCommand(
                registryPath,
                entry.Text,
                iconPath,
                $"\"{executablePath}\" {entry.Argument} {targetExpression}",
                appliesTo: entry.AppliesTo);
        }
    }

    private readonly record struct MenuCommandEntry(string Text, string Argument, string? AppliesTo = null);
}
