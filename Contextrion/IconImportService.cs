using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Contextrion;

internal sealed record IconImportResult(int CopiedCount, int ConvertedCount, IReadOnlyList<string> FailedFiles);

internal static class IconImportService
{
    private static readonly int[] IconOutputSizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];

    public static IconImportResult ImportFromDialog(IWin32Window owner)
    {
        using var dialog = new OpenFileDialog
        {
            Title = Translate("import.icons", "Import Icons"),
            Filter = Translate("file-dialog.icon-import-filter", "Supported files|*.ico;*.dll;*.png;*.jpg;*.jpeg|Icon files|*.ico|Dynamic libraries|*.dll|Images|*.png;*.jpg;*.jpeg|All files|*.*"),
            Multiselect = true
        };

        if (dialog.ShowDialog(owner) != DialogResult.OK || dialog.FileNames.Length == 0)
        {
            return new IconImportResult(0, 0, Array.Empty<string>());
        }

        AppPaths.EnsureDirectory(AppPaths.UserIconsDirectory);

        var copied = 0;
        var converted = 0;
        var failed = new List<string>();

        foreach (var filePath in dialog.FileNames)
        {
            try
            {
                var destinationName = BuildDestinationName(filePath);
                var destinationPath = MakeUniquePath(AppPaths.UserIconsDirectory, destinationName);

                if (filePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(filePath, destinationPath, overwrite: false);
                    copied++;
                    continue;
                }

                using var bitmap = LoadSquareBitmap(filePath, 256);
                SaveBitmapAsIcon(bitmap, destinationPath);
                converted++;
            }
            catch
            {
                failed.Add(Path.GetFileName(filePath));
            }
        }

        return new IconImportResult(copied, converted, failed);
    }

    public static Bitmap LoadImageForLayer(string filePath)
    {
        if (filePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            using var icon = new Icon(filePath, 256, 256);
            return icon.ToBitmap();
        }

        if (filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            var large = new IntPtr[1];
            var count = NativeMethods.ExtractIconEx(filePath, 0, large, null, 1);
            if (count <= 0 || large[0] == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    Translate("import.no-icon-from-dll", "No icon could be extracted from the selected DLL."));
            }

            try
            {
                using var icon = Icon.FromHandle(large[0]);
                return icon.ToBitmap();
            }
            finally
            {
                NativeMethods.DestroyIcon(large[0]);
            }
        }

        return LoadSquareBitmap(filePath, 256);
    }

    public static void SaveBitmapAsIcon(Bitmap bitmap, string targetPath)
    {
        var iconImages = IconOutputSizes
            .Select(size => new IconImage(size, EncodePng(FolderIconCatalog.ResizeToSquareBitmap(bitmap, size))))
            .ToList();

        using var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(output);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)iconImages.Count);

        var imageOffset = 6 + (16 * iconImages.Count);
        foreach (var image in iconImages)
        {
            writer.Write((byte)(image.Size >= 256 ? 0 : image.Size));
            writer.Write((byte)(image.Size >= 256 ? 0 : image.Size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(image.PngBytes.Length);
            writer.Write(imageOffset);
            imageOffset += image.PngBytes.Length;
        }

        foreach (var image in iconImages)
        {
            writer.Write(image.PngBytes);
        }
    }

    private static string BuildDestinationName(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Path.GetFileNameWithoutExtension(sourcePath)}.ico";
        }

        return Path.GetFileName(sourcePath);
    }

    private static string MakeUniquePath(string directory, string fileName)
    {
        var fullPath = Path.Combine(directory, fileName);
        if (!File.Exists(fullPath))
        {
            return fullPath;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var suffix = 1; suffix < 10000; suffix++)
        {
            fullPath = Path.Combine(directory, $"{stem} ({suffix}){extension}");
            if (!File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new IOException(
            Translate("import.unique-file-name-failed", "Could not create a unique file name for: {0}", fileName));
    }

    private static Bitmap LoadSquareBitmap(string filePath, int size)
    {
        using var image = Image.FromFile(filePath);
        return FolderIconCatalog.ResizeToSquareBitmap(image, size);
    }

    private static byte[] EncodePng(Bitmap bitmap)
    {
        using (bitmap)
        using (var pngStream = new MemoryStream())
        {
            bitmap.Save(pngStream, ImageFormat.Png);
            return pngStream.ToArray();
        }
    }

    private sealed record IconImage(int Size, byte[] PngBytes);
}
