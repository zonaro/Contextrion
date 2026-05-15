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

    public static Bitmap LoadCurrentFolderIconBitmap(string folderPath, int size)
    {
        if (folderPath.IsDirectoryPath() == false)
        {
            throw new DirectoryNotFoundException(
                Translate("errors.invalid-folder", "Invalid folder: {0}", folderPath));
        }

        var currentEntry = GetCurrentIconEntry(folderPath);
        if (currentEntry is not null)
        {
            try
            {
                return FolderIconCatalog.LoadBitmap(currentEntry, size);
            }
            catch
            {
                // Fall back to the Shell icon if the desktop.ini icon points to a missing or unreadable file.
            }
        }

        return LoadShellFolderIconBitmap(folderPath, size);
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

    private static FolderIconEntry? GetCurrentIconEntry(string folderPath)
    {
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        if (!File.Exists(desktopIniPath))
        {
            return null;
        }

        var iconResource = NativeMethods.ReadIniValue(ShellClassSection, IconResourceKey, desktopIniPath);
        if (!TryParseIconResource(iconResource, folderPath, out var iconPath, out var resourceIndex))
        {
            return null;
        }

        return new FolderIconEntry(
            Translate("folder-picker.current-folder-icon", "Current folder icon"),
            iconPath,
            resourceIndex,
            FolderIconSourceKind.User,
            "Imported Icons");
    }

    private static bool TryParseIconResource(string iconResource, string folderPath, out string iconPath, out int resourceIndex)
    {
        iconPath = string.Empty;
        resourceIndex = 0;

        var value = iconResource.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separatorIndex = value.LastIndexOf(',');
        if (separatorIndex > 0 && int.TryParse(value[(separatorIndex + 1)..].Trim(), out var parsedIndex))
        {
            resourceIndex = parsedIndex;
            value = value[..separatorIndex];
        }

        value = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        iconPath = Path.IsPathRooted(value)
            ? value
            : Path.GetFullPath(Path.Combine(folderPath, value));
        return true;
    }

    private static Bitmap LoadShellFolderIconBitmap(string folderPath, int size)
    {
        var result = NativeMethods.SHGetFileInfo(
            folderPath,
            0,
            out var fileInfo,
            (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
            NativeMethods.ShgfiIcon | NativeMethods.ShgfiLargeIcon);

        if (result == nint.Zero || fileInfo.hIcon == nint.Zero)
        {
            throw new InvalidOperationException(
                Translate("folder-picker.current-preview-unavailable", "Current folder icon preview unavailable."));
        }

        try
        {
            using var icon = Icon.FromHandle(fileInfo.hIcon);
            using var bitmap = icon.ToBitmap();
            return FolderIconCatalog.ResizeToSquareBitmap(bitmap, size);
        }
        finally
        {
            NativeMethods.DestroyIcon(fileInfo.hIcon);
        }
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
