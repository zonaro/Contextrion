using System.Text;

namespace Contextrion;

internal static class FolderCustomizationService
{
    private const string ShellClassSection = ".ShellClassInfo";
    private const string IconResourceKey = "IconResource";
    private const string DirectoryClassKey = "DirectoryClass";

    public static void ApplyIcon(string folderPath, FolderIconEntry entry)
    {
        if (folderPath.IsDirectoryPath()==false)
        {
            throw new DirectoryNotFoundException(
                Translate("errors.invalid-folder", "Invalid folder: {0}", folderPath));
        }

        AppPaths.EnsureDirectory(AppPaths.UserIconsDirectory);

        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        var iconPath = BundledAssetCatalog.EnsureIconFile(entry);
        var iconSpec = iconPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
            ? iconPath
            : $"{iconPath},{entry.ResourceIndex}";

        EnsureFolderIsSystem(folderPath);
        PreserveExistingDesktopIni(folderPath, desktopIniPath);

        NativeMethods.WritePrivateProfileString(ShellClassSection, IconResourceKey, iconSpec, desktopIniPath);
        NativeMethods.WritePrivateProfileString(ShellClassSection, DirectoryClassKey, AppPaths.CustomFolderClass, desktopIniPath);

        if (File.Exists(desktopIniPath))
        {
            File.SetAttributes(desktopIniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
        }

        RefreshShell(folderPath);
    }

    public static void RestoreDefault(string folderPath)
    {
        if (folderPath.IsDirectoryPath() == false)
        {
            throw new DirectoryNotFoundException(
                Translate("errors.invalid-folder", "Invalid folder: {0}", folderPath));
        }

        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        if (!File.Exists(desktopIniPath))
        {
            RefreshShell(folderPath);
            return;
        }

        NativeMethods.WritePrivateProfileString(ShellClassSection, IconResourceKey, null, desktopIniPath);

        var directoryClass = NativeMethods.ReadIniValue(ShellClassSection, DirectoryClassKey, desktopIniPath);
        if (string.Equals(directoryClass, AppPaths.CustomFolderClass, StringComparison.OrdinalIgnoreCase))
        {
            NativeMethods.WritePrivateProfileString(ShellClassSection, DirectoryClassKey, null, desktopIniPath);
        }

        if (!HasSectionContent(desktopIniPath, ShellClassSection))
        {
            NativeMethods.WritePrivateProfileString(ShellClassSection, null, null, desktopIniPath);
        }

        var content = File.ReadAllText(desktopIniPath, Encoding.Unicode);
        if (string.IsNullOrWhiteSpace(content.Replace("\0", string.Empty).Trim()))
        {
            File.SetAttributes(desktopIniPath, FileAttributes.Normal);
            File.Delete(desktopIniPath);
        }

        RefreshShell(folderPath);
    }

    public static void RefreshShell(string? path = null)
    {
        NativeMethods.SHChangeNotify(NativeMethods.ShcneAssocChanged, NativeMethods.ShcnfIdList | NativeMethods.ShcnfFlushNoWait, null, 0);
        if (!string.IsNullOrWhiteSpace(path))
        {
            NativeMethods.SHChangeNotify(NativeMethods.ShcneUpdateDir, NativeMethods.ShcnfPathW | NativeMethods.ShcnfFlushNoWait, path, 0);
        }

        _ = NativeMethods.SendMessageTimeout(-1, NativeMethods.WmSettingChange, 0, @"Software\Classes", NativeMethods.SmtoAbortIfHung, 5000, out _);
        _ = NativeMethods.SendMessageTimeout(-1, NativeMethods.WmSettingChange, 0, "Shell Icons", NativeMethods.SmtoAbortIfHung, 5000, out _);
    }

    private static void EnsureFolderIsSystem(string folderPath)
    {
        var attributes = File.GetAttributes(folderPath);
        if (!attributes.HasFlag(FileAttributes.System))
        {
            File.SetAttributes(folderPath, attributes | FileAttributes.System);
        }
    }

    private static void PreserveExistingDesktopIni(string folderPath, string desktopIniPath)
    {
        if (!File.Exists(desktopIniPath))
        {
            File.WriteAllText(desktopIniPath, $"[{ShellClassSection}]{Environment.NewLine}", Encoding.Unicode);
            return;
        }

        var attributes = File.GetAttributes(desktopIniPath);
        if (attributes.HasFlag(FileAttributes.ReadOnly) || attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System))
        {
            File.SetAttributes(desktopIniPath, FileAttributes.Normal);
        }

        var text = File.ReadAllText(desktopIniPath, Encoding.Unicode);
        if (!text.Contains($"[{ShellClassSection}]", StringComparison.OrdinalIgnoreCase))
        {
            File.AppendAllText(desktopIniPath, $"{Environment.NewLine}[{ShellClassSection}]{Environment.NewLine}", Encoding.Unicode);
        }
    }

    private static bool HasSectionContent(string desktopIniPath, string section)
    {
        var content = File.ReadAllText(desktopIniPath, Encoding.Unicode);
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None);
        var insideSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                insideSection = string.Equals(trimmed, $"[{section}]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (insideSection && !string.IsNullOrWhiteSpace(trimmed) && trimmed.Contains('='))
            {
                return true;
            }
        }

        return false;
    }
}
