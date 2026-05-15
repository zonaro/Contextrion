using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Contextrion;

internal static class FolderIconCatalog
{
    private static readonly int[] PreferredIconSizes = [256, 128, 96, 64, 48, 40, 32, 24, 20, 16];

    private static readonly string[] BuiltInCategoryOrder =
    [
        "Current Windows",
        "Windows 11",
        "Windows 10",
        "Windows 7/8",
        "Imported Icons",
        "Icon Packs"
    ];

    public static IReadOnlyList<string> GetCategoryOrder() => BuiltInCategoryOrder;

    public static string GetCategoryDisplayName(string category)
    {
        return category switch
        {
            "Current Windows" => Translate("folder-catalog.category.current-windows", "Windows Atual"),
            "Windows 11" => Translate("folder-catalog.category.windows-11", "Windows 11"),
            "Windows 10" => Translate("folder-catalog.category.windows-10", "Windows 10"),
            "Windows 7/8" => Translate("folder-catalog.category.windows-7-8", "Windows 7/8"),
            "Imported Icons" => Translate("folder-catalog.category.imported-icons", "Ícones Importados"),
            "Icon Packs" => Translate("folder-catalog.category.icon-packs", "Pacotes de Ícones"),
            _ => category
        };
    }

    public static IReadOnlyList<FolderIconEntry> LoadAll()
    {
        var entries = new List<FolderIconEntry>();
        entries.AddRange(LoadBuiltInEntries());
        entries.AddRange(LoadUserEntries());
        entries.AddRange(LoadPackEntries());
        return entries;
    }

    private static IEnumerable<FolderIconEntry> LoadBuiltInEntries()
    {
        var currentCategory = GetCurrentWindowsCategory();
        var categoryByFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Win11set"] = "Windows 11",
            ["Win10set"] = "Windows 10",
            ["Win7_8set"] = "Windows 7/8"
        };

        foreach (var (folderName, resourceName, label) in BundledAssetCatalog.EnumerateBuiltInIcons())
        {
            if (!categoryByFolder.TryGetValue(folderName, out var categoryName))
            {
                continue;
            }

            var resourcePath = BundledAssetCatalog.GetBundledIconPath(resourceName);
            yield return new FolderIconEntry(label, resourcePath, 0, FolderIconSourceKind.BuiltIn, categoryName);
            if (string.Equals(categoryName, currentCategory, StringComparison.OrdinalIgnoreCase))
            {
                yield return new FolderIconEntry(label, resourcePath, 0, FolderIconSourceKind.BuiltIn, "Current Windows", true);
            }
        }
    }

    private static IEnumerable<FolderIconEntry> LoadUserEntries()
    {
        var directory = AppPaths.UserIconsDirectory;
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var filePath in Directory.GetFiles(directory, "*.ico", SearchOption.AllDirectories).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var relativeDirectory = Path.GetRelativePath(directory, Path.GetDirectoryName(filePath) ?? directory);
            var label = relativeDirectory == "."
                ? Path.GetFileNameWithoutExtension(filePath)
                : $"{relativeDirectory} / {Path.GetFileNameWithoutExtension(filePath)}";

            yield return new FolderIconEntry(label, filePath, 0, FolderIconSourceKind.User, "Imported Icons");
        }
    }

    private static IEnumerable<FolderIconEntry> LoadPackEntries()
    {
        var root = AppPaths.CustomPacksDirectory;
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var filePath in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories)
                     .Where(static path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(root, filePath);
            if (filePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                yield return new FolderIconEntry(Path.GetFileNameWithoutExtension(relative), filePath, 0, FolderIconSourceKind.Pack, "Icon Packs");
                continue;
            }

            var iconCount = NativeMethods.ExtractIconEx(filePath, -1, null, null, 0);
            for (var iconIndex = 0; iconIndex < iconCount; iconIndex++)
            {
                yield return new FolderIconEntry(
                    Translate(
                        "folder-catalog.pack-icon-label",
                        "{0} / Ícone {1}",
                        Path.GetFileNameWithoutExtension(relative),
                        iconIndex.ToString("000")),
                    filePath,
                    iconIndex,
                    FolderIconSourceKind.Pack,
                    "Icon Packs");
            }
        }
    }

    public static Icon? LoadIcon(FolderIconEntry entry, int size = 32)
    {
        if (BundledAssetCatalog.IsBundledIconPath(entry.ResourcePath))
        {
            using var stream = BundledAssetCatalog.OpenBundledIconStream(entry.ResourcePath);
            using var source = new Icon(stream, size, size);
            return (Icon)source.Clone();
        }

        if (entry.ResourcePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            using var source = new Icon(entry.ResourcePath, size, size);
            return (Icon)source.Clone();
        }

        foreach (var candidateSize in GetIconSizeCandidates(size))
        {
            var handles = new IntPtr[1];
            var privateExtracted = NativeMethods.PrivateExtractIcons(
                entry.ResourcePath,
                entry.ResourceIndex,
                candidateSize,
                candidateSize,
                handles,
                null,
                1,
                0);

            if (privateExtracted <= 0 || handles[0] == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                using var source = Icon.FromHandle(handles[0]);
                return (Icon)source.Clone();
            }
            finally
            {
                NativeMethods.DestroyIcon(handles[0]);
            }
        }

        var large = new IntPtr[1];
        var extracted = NativeMethods.ExtractIconEx(entry.ResourcePath, entry.ResourceIndex, large, null, 1);
        if (extracted <= 0 || large[0] == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            using var source = Icon.FromHandle(large[0]);
            return (Icon)source.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(large[0]);
        }
    }

    public static Bitmap LoadBitmap(FolderIconEntry entry, int size = 256)
    {
        using var icon = LoadIcon(entry, size)
            ?? throw new InvalidOperationException(
                Translate("folder-catalog.icon-load-failed", "Could not load icon: {0}", entry.Label));
        using var bitmap = icon.ToBitmap();
        return ResizeToSquareBitmap(bitmap, size);
    }

    public static Bitmap ResizeToSquareBitmap(Image image, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        var scale = Math.Min(size / (float)image.Width, size / (float)image.Height);
        var width = (int)Math.Round(image.Width * scale);
        var height = (int)Math.Round(image.Height * scale);
        var x = (size - width) / 2;
        var y = (size - height) / 2;
        graphics.DrawImage(image, new Rectangle(x, y, width, height));
        return bitmap;
    }

    public static string GetCurrentWindowsCategory()
    {
        return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
            ? "Windows 11"
            : OperatingSystem.IsWindowsVersionAtLeast(10)
                ? "Windows 10"
                : "Windows 7/8";
    }

    private static IEnumerable<int> GetIconSizeCandidates(int requestedSize)
    {
        yield return requestedSize;

        foreach (var size in PreferredIconSizes)
        {
            if (size < requestedSize)
            {
                yield return size;
            }
        }
    }
}
