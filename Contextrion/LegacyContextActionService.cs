using System.Buffers.Binary;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Contextrion;

internal static class LegacyContextActionService
{
    public static void Execute(AppMode mode, IReadOnlyList<string> rawTargetPaths)
    {
        var targetPaths = NormalizePaths(rawTargetPaths);
        if (targetPaths.Count == 0)
        {
            MessageBox.Show(
                Translate("legacy.no-valid-targets", "No valid files or folders were selected."),
                Translate("titles.clipboard-files", "Clipboard Files"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        switch (mode)
        {
            case AppMode.FriendlyName:
                RenameToFriendlyName(targetPaths);
                return;
            case AppMode.ToDataUrl:
                CopyAsDataUrl(targetPaths);
                return;
            case AppMode.CopyPath:
                CopyFirstPath(targetPaths);
                return;
            case AppMode.EnumRename:
                RenameWithPattern(targetPaths);
                return;
            case AppMode.CombineVertical:
                CombineImages(targetPaths, verticalFlow: true);
                return;
            case AppMode.CombineHorizontal:
                CombineImages(targetPaths, verticalFlow: false);
                return;
            case AppMode.CleanEmpty:
                CleanEmptyDirectories(targetPaths);
                return;
            case AppMode.CopyContent:
                CopyContent(targetPaths);
                return;
            case AppMode.Grayscale:
                ProcessImages(targetPaths, "_grayscale", image => image.Grayscale());
                return;
            case AppMode.Watermark:
                ApplyWatermark(targetPaths);
                return;
            case AppMode.Crop:
                CropImages(targetPaths);
                return;
            case AppMode.Circle:
                ProcessImages(targetPaths, "_circle", image => image.CropToCircle());
                return;
            case AppMode.Resize:
                ResizeImages(targetPaths);
                return;
            case AppMode.InvertColor:
                ProcessImages(targetPaths, "_inverted", image => image.InvertColors());
                return;
            case AppMode.CleanMetadata:
                CleanMetadata(targetPaths);
                return;
            case AppMode.Minify:
                MinifyFiles(targetPaths);
                return;
            case AppMode.Optimize:
                OptimizeImages(targetPaths);
                return;
            default:
                throw new InvalidOperationException(
                    Translate("legacy.unsupported-action", "Unsupported action: {0}", mode.ToString()));
        }
    }

    private static IReadOnlyList<string> NormalizePaths(IEnumerable<string> rawTargetPaths)
    {
        return rawTargetPaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(static path => path.Trim())
            .Where(static path => File.Exists(path) || Directory.Exists(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void RenameToFriendlyName(IReadOnlyList<string> targetPaths)
    {
        var files = ExpandFiles(targetPaths).ToList();
        if (files.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.no-files-to-rename", "No files were found to rename."));
            return;
        }

        if (!Confirm(Translate("legacy.confirm-friendly-rename", "Rename {0} file(s) to a friendly URL name?", files.Count.ToString())))
        {
            return;
        }

        var renamed = 0;
        foreach (var file in files)
        {
            var newName = Path.GetFileNameWithoutExtension(file.Name).ToFriendlyURL(true) + file.Extension;
            if (TryRename(file, newName))
            {
                renamed++;
            }
        }

            MessageBox.Show(
            Translate("legacy.renamed-files", "Renamed {0} file(s).", renamed.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void CopyAsDataUrl(IReadOnlyList<string> targetPaths)
    {
        var files = ExpandFiles(targetPaths).ToList();
        if (files.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.select-file-data-url", "Select at least one file to copy as Data URL."));
            return;
        }

        if (files.Count == 1)
        {
            Clipboard.SetText(files[0].ToDataURL());
            MessageBox.Show(
                Translate("legacy.copied-data-url", "{0} copied as Data URL.", files[0].Name),
                Translate("titles.clipboard-files", "Clipboard Files"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(string.Join(Environment.NewLine + Environment.NewLine, files.Select(static file => file.ToDataURL())));
        MessageBox.Show(
            Translate("legacy.copied-data-url-multiple", "Copied Data URLs for {0} file(s).", files.Count.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void CopyFirstPath(IReadOnlyList<string> targetPaths)
    {
        var filePaths = ExpandFiles(targetPaths)
            .Select(static file => file.FullName)
            .ToList();

        var pathsToCopy = filePaths.Count > 0 ? filePaths : targetPaths.ToList();
        var pathMessageKey = pathsToCopy.Count == 1 ? "legacy.path-copied" : "legacy.paths-copied";
        var pathMessageDefault = pathsToCopy.Count == 1
            ? "The selected path was copied to the clipboard."
            : "Copied {0} paths to the clipboard.";
        var pathMessageArgs = pathsToCopy.Count == 1
            ? Array.Empty<string>()
            : [pathsToCopy.Count.ToString()];

        Clipboard.SetText(string.Join(Environment.NewLine, pathsToCopy));
        MessageBox.Show(
            Translate(pathMessageKey, pathMessageDefault, pathMessageArgs),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void RenameWithPattern(IReadOnlyList<string> targetPaths)
    {
        var files = ExpandFiles(targetPaths).ToList();
        if (files.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.no-files-to-rename", "No files were found to rename."));
            return;
        }

        var pattern = PromptDialog.Show(
            Translate("legacy.prompt.rename-pattern", "Enter the new file name pattern.\nUse # for numbering and $ to reuse the old file name."),
            Translate("titles.bulk-rename", "Bulk Rename"),
            Translate("legacy.rename-pattern-default", "file (#)"));

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        if (!pattern.Contains('#'))
        {
            pattern += " (#)";
        }

        var renamed = 0;
        var number = 0;
        foreach (var file in files)
        {
            number++;
            var newName = pattern.Replace("#", number.ToString(), StringComparison.Ordinal)
                .Replace("$", Path.GetFileNameWithoutExtension(file.Name), StringComparison.Ordinal)
                + file.Extension;

            if (TryRename(file, newName))
            {
                renamed++;
            }
        }

            MessageBox.Show(
            Translate("legacy.renamed-files", "Renamed {0} file(s).", renamed.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void CombineImages(IReadOnlyList<string> targetPaths, bool verticalFlow)
    {
        var imageFiles = GetImageFiles(targetPaths).ToList();
        if (imageFiles.Count < 2)
        {
            ShowNothingToDo(Translate("legacy.select-two-images", "Select at least two image files to combine."));
            return;
        }

        using var images = new DisposableImageCollection(imageFiles.Select(LoadBitmapCopy));
        using var combined = images.Images.CombineImages(verticalFlow);
        var outputDirectory = imageFiles[0].DirectoryName ?? Path.GetDirectoryName(imageFiles[0].FullName) ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var outputPath = GetUniqueOutputPath(
            outputDirectory,
            Translate("legacy.output.combine-name", "combined_images"),
            ".png");
        combined.Save(outputPath, ImageFormat.Png);

            MessageBox.Show(
            Translate("legacy.combined-image-saved", "Combined image saved to:\n{0}", outputPath),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void CleanEmptyDirectories(IReadOnlyList<string> targetPaths)
    {
        var directories = targetPaths.Where(Directory.Exists).Select(path => new DirectoryInfo(path)).ToList();
        if (directories.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.select-folder-clean", "Select at least one folder to clean."));
            return;
        }

        if (!Confirm(Translate("legacy.confirm-clean-empty", "This will remove all empty directories inside the selected folder(s). Continue?")))
        {
            return;
        }

        var deleted = 0;
        foreach (var directory in directories)
        {
            deleted += directory.CleanDirectory().Count();
        }

            MessageBox.Show(
            Translate(
                "legacy.removed-empty-directories",
                "Removed {0} empty director{1}.",
                deleted.ToString(),
                deleted == 1 ? "y" : "ies"),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void CopyContent(IReadOnlyList<string> targetPaths)
    {
        var textBuilder = new StringBuilder();
        var imageFiles = new List<FileInfo>();

        foreach (var file in ExpandFiles(targetPaths))
        {
            var fileType = new FileType(file);
            if (fileType.IsText())
            {
                textBuilder.Append(File.ReadAllText(file.FullName));
                continue;
            }

            if (fileType.IsImage())
            {
                imageFiles.Add(file);
                continue;
            }

            if (fileType.IsAudio())
            {
                using var stream = new MemoryStream(File.ReadAllBytes(file.FullName));
                Clipboard.SetAudio(stream);
                MessageBox.Show(
                    Translate("legacy.audio-copied", "{0} copied to the clipboard as audio.", file.Name),
                    Translate("titles.clipboard-files", "Clipboard Files"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
        }

        if (imageFiles.Count > 0)
        {
            using var images = new DisposableImageCollection(imageFiles.Select(LoadBitmapCopy));
            if (textBuilder.Length > 0)
            {
                var textImageWidth = Math.Max(800, images.Images.Max(static image => image.Width));
                var textLineCount = textBuilder.ToString().Split(["\r\n", "\n"], StringSplitOptions.None).Length;
                var textImageHeight = Math.Max(120, textLineCount * 48);
                images.Add((Bitmap)textBuilder.ToString().DrawImage(textImageWidth, textImageHeight));
            }

            using var outputImage = images.Images.Count == 1
                ? new Bitmap(images.Images[0])
                : new Bitmap(images.Images.CombineImages(Confirm(Translate("legacy.confirm-combine-copied-images", "Combine the copied images vertically?"))));

            Clipboard.SetImage(outputImage);
            MessageBox.Show(
                Translate("legacy.image-content-copied", "Image content copied to the clipboard."),
                Translate("titles.clipboard-files", "Clipboard Files"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (textBuilder.Length > 0)
        {
            Clipboard.SetText(textBuilder.ToString());
            MessageBox.Show(
                Translate("legacy.text-content-copied", "Text content copied to the clipboard."),
                Translate("titles.clipboard-files", "Clipboard Files"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ShowNothingToDo(Translate("legacy.no-supported-files", "No supported text, image, or audio files were found."));
    }

    private static void ApplyWatermark(IReadOnlyList<string> targetPaths)
    {
        var imageFiles = GetImageFiles(targetPaths).ToList();
        if (imageFiles.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.select-image-file", "Select at least one image file."));
            return;
        }

        var watermarkInput = PromptDialog.Show(
            Translate("legacy.prompt.watermark", "Enter the watermark text or the full path of an image to use as a watermark."),
            Translate("titles.watermark", "Watermark"));

        if (string.IsNullOrWhiteSpace(watermarkInput))
        {
            return;
        }

        using var watermark = CreateWatermarkImage(watermarkInput);
        var saved = 0;
        foreach (var file in imageFiles)
        {
            using var image = LoadBitmapCopy(file);
            using var result = image.Watermark(watermark);
            SaveDerivedImage(file, result, "_wtmrk");
            saved++;
        }

        MessageBox.Show(
            Translate("legacy.saved-watermarked-images", "Saved {0} watermarked image(s).", saved.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void CropImages(IReadOnlyList<string> targetPaths)
    {
        var sizeText = PromptDialog.Show(
            Translate("legacy.prompt.crop-size", "Enter the crop size (example: 200x200)."),
            Translate("titles.crop-images", "Crop Images"),
            "200x200");
        if (string.IsNullOrWhiteSpace(sizeText))
        {
            return;
        }

        var size = ParseSizeOrThrow(sizeText);
        ProcessImages(targetPaths, "_" + SanitizeFileToken(sizeText), image => image.Crop(size));
    }

    private static void ResizeImages(IReadOnlyList<string> targetPaths)
    {
        var sizeText = PromptDialog.Show(
            Translate("legacy.prompt.resize-size", "Enter the new image size (example: 200x200)."),
            Translate("titles.resize-images", "Resize Images"),
            "200x200");
        if (string.IsNullOrWhiteSpace(sizeText))
        {
            return;
        }

        var size = ParseSizeOrThrow(sizeText);
        ProcessImages(targetPaths, "_" + SanitizeFileToken(sizeText), image => image.Resize(size.Width, size.Height, false));
    }

    private static void MinifyFiles(IReadOnlyList<string> targetPaths)
    {
        var files = ExpandFiles(targetPaths)
            .Where(static file => file.Extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
                                  file.Extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (files.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.select-js-css", "Select at least one .js or .css file."));
            return;
        }

        var saved = 0;
        foreach (var file in files)
        {
            var content = File.ReadAllText(file.FullName);
            var minified = file.Extension.Equals(".css", StringComparison.OrdinalIgnoreCase)
                ? content.MinifyCSS()
                : MinifyJavaScript(content);

            var outputPath = Path.Combine(
                file.DirectoryName ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"{Path.GetFileNameWithoutExtension(file.Name)}.min{file.Extension}");

            File.WriteAllText(outputPath, minified, Encoding.UTF8);
            saved++;
        }

        MessageBox.Show(
            Translate("legacy.created-minified-files", "Created {0} minified file(s).", saved.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void CleanMetadata(IReadOnlyList<string> targetPaths)
    {
        var imageFiles = ExpandFiles(targetPaths)
            .Where(static file => IsMetadataCleaningSupported(file.Extension))
            .ToList();

        if (imageFiles.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.select-image-clean-metadata", "Select at least one .jpg, .jpeg, or .png file."));
            return;
        }

        var saved = 0;
        foreach (var file in imageFiles)
        {
            SaveMetadataCleanCopy(file);
            saved++;
        }

        MessageBox.Show(
            Translate("legacy.saved-cleaned-images", "Saved {0} cleaned image(s).", saved.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void OptimizeImages(IReadOnlyList<string> targetPaths)
    {
        var imageFiles = GetImageFiles(targetPaths).ToList();
        if (imageFiles.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.select-image-file", "Select at least one image file."));
            return;
        }

        var sizeText = PromptDialog.Show(
            Translate("legacy.prompt.optimize-size", "Enter the maximum base size in pixels."),
            Translate("titles.optimize-images", "Optimize Images"),
            "150");
        if (!int.TryParse(sizeText, out var maxSize) || maxSize <= 0)
        {
            return;
        }

        var qualityText = PromptDialog.Show(
            Translate("legacy.prompt.optimize-quality", "Enter the JPEG quality (1-100)."),
            Translate("titles.optimize-images", "Optimize Images"),
            "75");
        if (!long.TryParse(qualityText, out var quality))
        {
            return;
        }

        quality = Math.Clamp(quality, 1, 100);
        var saved = 0;
        foreach (var file in imageFiles)
        {
            using var image = LoadBitmapCopy(file);
            using var optimized = ResizeToFit(image, maxSize);
            SaveOptimizedImage(file, optimized, quality);
            saved++;
        }

        MessageBox.Show(
            Translate("legacy.optimized-images", "Optimized {0} image(s).", saved.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void ProcessImages(IReadOnlyList<string> targetPaths, string suffix, Func<Image, Image> transform)
    {
        var imageFiles = GetImageFiles(targetPaths).ToList();
        if (imageFiles.Count == 0)
        {
            ShowNothingToDo(Translate("legacy.select-image-file", "Select at least one image file."));
            return;
        }

        var saved = 0;
        foreach (var file in imageFiles)
        {
            using var image = LoadBitmapCopy(file);
            using var result = transform(image);
            SaveDerivedImage(file, result, suffix);
            saved++;
        }

        MessageBox.Show(
            Translate("legacy.saved-processed-images", "Saved {0} processed image(s).", saved.ToString()),
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static IEnumerable<FileInfo> ExpandFiles(IEnumerable<string> targetPaths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in targetPaths)
        {
            if (File.Exists(path))
            {
                var fullPath = Path.GetFullPath(path);
                if (seen.Add(fullPath))
                {
                    yield return new FileInfo(fullPath);
                }
                continue;
            }

            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                var fullPath = Path.GetFullPath(file);
                if (seen.Add(fullPath))
                {
                    yield return new FileInfo(fullPath);
                }
            }
        }
    }

    private static IEnumerable<FileInfo> ExpandDirectFiles(IEnumerable<string> targetPaths)
    {
        return targetPaths
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static path => new FileInfo(path));
    }

    private static IEnumerable<FileInfo> GetImageFiles(IEnumerable<string> targetPaths)
    {
        return ExpandFiles(targetPaths).Where(static file => new FileType(file).IsImage());
    }

    private static bool TryRename(FileInfo file, string proposedName)
    {
        if (string.IsNullOrWhiteSpace(proposedName))
        {
            return false;
        }

        var sanitizedName = string.Concat(proposedName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return false;
        }

        var destinationPath = GetUniqueSiblingPath(file.DirectoryName ?? string.Empty, sanitizedName);
        if (string.Equals(file.FullName, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        File.Move(file.FullName, destinationPath);
        return true;
    }

    private static string GetUniqueSiblingPath(string directoryPath, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidatePath = Path.Combine(directoryPath, fileName);
        var index = 2;

        while (File.Exists(candidatePath))
        {
            candidatePath = Path.Combine(directoryPath, $"{baseName} ({index}){extension}");
            index++;
        }

        return candidatePath;
    }

    private static string GetUniqueOutputPath(string directoryPath, string baseName, string extension)
    {
        var candidatePath = Path.Combine(directoryPath, baseName + extension);
        var index = 2;

        while (File.Exists(candidatePath))
        {
            candidatePath = Path.Combine(directoryPath, $"{baseName} ({index}){extension}");
            index++;
        }

        return candidatePath;
    }

    private static Bitmap LoadBitmapCopy(FileInfo file)
    {
        using var stream = file.OpenRead();
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static void SaveMetadataCleanCopy(FileInfo sourceFile)
    {
        using var original = LoadBitmapCopy(sourceFile);
        var normalizedExtension = NormalizeCleanImageExtension(sourceFile.Extension);
        var outputPath = GetUniqueSiblingPath(
            sourceFile.DirectoryName ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            $"{Path.GetFileNameWithoutExtension(sourceFile.Name)}_cleaned{normalizedExtension}");

        if (normalizedExtension == ".jpg")
        {
            using var cleaned = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb);

            using (var graphics = Graphics.FromImage(cleaned))
            {
                graphics.Clear(Color.White);
                graphics.DrawImage(original, 0, 0, original.Width, original.Height);
            }

            File.WriteAllBytes(outputPath, EncodeMetadataFreeJpeg(cleaned, 92L));
            return;
        }

        using (var cleaned = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb))
        {
            using var graphics = Graphics.FromImage(cleaned);
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(original, 0, 0, original.Width, original.Height);
            File.WriteAllBytes(outputPath, EncodeMetadataFreePng(cleaned));
        }
    }

    private static void SaveDerivedImage(FileInfo sourceFile, Image image, string suffix)
    {
        var extension = sourceFile.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        sourceFile.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            ? ".jpg"
            : ".png";

        var outputPath = Path.Combine(
            sourceFile.DirectoryName ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            $"{Path.GetFileNameWithoutExtension(sourceFile.Name)}{suffix}{extension}");

        if (extension == ".jpg")
        {
            SaveJpeg(outputPath, image, 92L);
            return;
        }

        image.Save(outputPath, ImageFormat.Png);
    }

    private static void SaveOptimizedImage(FileInfo sourceFile, Image image, long quality)
    {
        var outputPath = Path.Combine(
            sourceFile.DirectoryName ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            $"{Path.GetFileNameWithoutExtension(sourceFile.Name)}_optimized.jpg");

        using var flattened = FlattenIfTransparent(image);
        SaveJpeg(outputPath, flattened, quality);
    }

    private static Bitmap FlattenIfTransparent(Image image)
    {
        var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.DrawImage(image, 0, 0, image.Width, image.Height);
        return bitmap;
    }

    private static void SaveJpeg(string outputPath, Image image, long quality)
    {
        var jpegEncoder = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(static codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        if (jpegEncoder is null)
        {
            image.Save(outputPath, ImageFormat.Jpeg);
            return;
        }

        using var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
        image.Save(outputPath, jpegEncoder, encoderParameters);
    }

    private static byte[] EncodeMetadataFreeJpeg(Image image, long quality)
    {
        using var stream = new MemoryStream();
        var jpegEncoder = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(static codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        if (jpegEncoder is null)
        {
            image.Save(stream, ImageFormat.Jpeg);
        }
        else
        {
            using var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            image.Save(stream, jpegEncoder, encoderParameters);
        }

        return StripJpegMetadataSegments(stream.ToArray());
    }

    private static byte[] EncodeMetadataFreePng(Image image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);
        return StripPngAncillaryChunks(stream.ToArray());
    }

    private static byte[] StripJpegMetadataSegments(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
        {
            return data;
        }

        using var output = new MemoryStream(data.Length);
        output.Write(data, 0, 2);
        var position = 2;

        while (position < data.Length)
        {
            if (data[position] != 0xFF)
            {
                output.Write(data, position, data.Length - position);
                break;
            }

            var markerStart = position;
            while (position < data.Length && data[position] == 0xFF)
            {
                position++;
            }

            if (position >= data.Length)
            {
                break;
            }

            var marker = data[position++];
            if (marker == 0x00)
            {
                output.Write(data, markerStart, position - markerStart);
                continue;
            }

            if (marker == 0xD9)
            {
                output.Write(data, markerStart, position - markerStart);
                break;
            }

            if (marker == 0xDA)
            {
                output.Write(data, markerStart, data.Length - markerStart);
                break;
            }

            if (IsStandaloneJpegMarker(marker))
            {
                output.Write(data, markerStart, position - markerStart);
                continue;
            }

            if (position + 2 > data.Length)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position, 2));
            if (segmentLength < 2 || position + segmentLength > data.Length)
            {
                break;
            }

            var segmentEnd = position + segmentLength;
            if (!IsJpegMetadataMarker(marker))
            {
                output.Write(data, markerStart, segmentEnd - markerStart);
            }

            position = segmentEnd;
        }

        return output.ToArray();
    }

    private static bool IsStandaloneJpegMarker(byte marker)
    {
        return marker == 0x01 || marker is >= 0xD0 and <= 0xD7;
    }

    private static bool IsJpegMetadataMarker(byte marker)
    {
        return marker == 0xFE || marker is >= 0xE0 and <= 0xEF;
    }

    private static byte[] StripPngAncillaryChunks(byte[] data)
    {
        ReadOnlySpan<byte> pngSignature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (data.Length < pngSignature.Length || !data.AsSpan(0, pngSignature.Length).SequenceEqual(pngSignature))
        {
            return data;
        }

        using var output = new MemoryStream(data.Length);
        output.Write(data, 0, pngSignature.Length);
        var position = pngSignature.Length;

        while (position + 12 <= data.Length)
        {
            var chunkStart = position;
            var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position, 4));
            position += 4;

            if (chunkLength > int.MaxValue || position + 4 + (int)chunkLength + 4 > data.Length)
            {
                break;
            }

            var typeStart = position;
            var type = data.AsSpan(typeStart, 4);
            position += 4 + (int)chunkLength + 4;

            if (IsCriticalPngChunk(type))
            {
                output.Write(data, chunkStart, position - chunkStart);
            }

            if (type.SequenceEqual("IEND"u8))
            {
                break;
            }
        }

        return output.ToArray();
    }

    private static bool IsCriticalPngChunk(ReadOnlySpan<byte> type)
    {
        return type.Length == 4 && type[0] is >= (byte)'A' and <= (byte)'Z';
    }

    private static Bitmap ResizeToFit(Image image, int maxSize)
    {
        var largestSide = Math.Max(image.Width, image.Height);
        if (largestSide <= maxSize)
        {
            return new Bitmap(image);
        }

        var scale = maxSize / (double)largestSide;
        var newWidth = Math.Max(1, (int)Math.Round(image.Width * scale));
        var newHeight = Math.Max(1, (int)Math.Round(image.Height * scale));
        return new Bitmap(image.Resize(newWidth, newHeight, false));
    }

    private static bool IsMetadataCleaningSupported(string extension)
    {
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCleanImageExtension(string extension)
    {
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
    }

    private static void CopyImageResolution(Image source, Bitmap target)
    {
        if (source.HorizontalResolution <= 0 || source.VerticalResolution <= 0)
        {
            return;
        }

        target.SetResolution(source.HorizontalResolution, source.VerticalResolution);
    }

    private static Image CreateWatermarkImage(string watermarkInput)
    {
        if (File.Exists(watermarkInput) && new FileType(watermarkInput).IsImage())
        {
         
            return LoadBitmapCopy(new FileInfo(watermarkInput));
        }

        return watermarkInput.DrawImage(500, 120, Color: Color.Gray);
    }

    private static Size ParseSizeOrThrow(string value)
    {
        var size = value.ParseSize();
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new InvalidOperationException(
                Translate("legacy.invalid-size-format", "Invalid size format. Use values such as 200x200."));
        }

        return size;
    }

    private static string SanitizeFileToken(string value)
    {
        return string.Concat(value.Where(static c => !Path.GetInvalidFileNameChars().Contains(c))).Replace(' ', '_');
    }

    private static string MinifyJavaScript(string content)
    {
        content = Regex.Replace(content, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        content = Regex.Replace(content, @"^\s*//.*$", string.Empty, RegexOptions.Multiline);
        content = Regex.Replace(content, @"\s+", " ");
        return content.Trim();
    }

    private static bool Confirm(string message)
    {
        return MessageBox.Show(
            message,
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private static void ShowNothingToDo(string message)
    {
        MessageBox.Show(
            message,
            Translate("titles.clipboard-files", "Clipboard Files"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private sealed class DisposableImageCollection : IDisposable
    {
        public DisposableImageCollection(IEnumerable<Bitmap> images)
        {
            Images = images.ToList();
        }

        public List<Bitmap> Images { get; }

        public void Add(Bitmap bitmap)
        {
            Images.Add(bitmap);
        }

        public void Dispose()
        {
            foreach (var image in Images)
            {
                image.Dispose();
            }
        }
    }
}
