using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.IO.Compression;

namespace Contextrion;

internal static class ClipboardSaveService
{
    public static ClipboardContentSnapshot CaptureClipboardContent()
    {
        if (Clipboard.ContainsFileDropList())
        {
            var items = Clipboard.GetFileDropList().Cast<string>().Where(static path => !string.IsNullOrWhiteSpace(path)).ToArray();
            return new ClipboardContentSnapshot(
                ClipboardContentKind.Archive,
                ".zip",
                Translate("clipboard.display.archive", "Archive"),
                Translate("clipboard.contains-archive", "The clipboard contains files or folders. It will be saved as a ZIP archive."),
                fileItems: items);
        }

        var dataObject = Clipboard.GetDataObject();
        if (dataObject is not null && Clipboard.ContainsImage())
        {
            using var sourceImage = Clipboard.GetImage();
            if (sourceImage is null)
            {
                throw new InvalidOperationException(Translate("clipboard.image-read-failed", "The clipboard image could not be read."));
            }

            var previewImage = new Bitmap(sourceImage);
            if (TryGetRawImageBytes(dataObject, out var extension, out var rawImageBytes))
            {
                var formatName = extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ? "JPEG image" : "PNG image";
                return new ClipboardContentSnapshot(
                    ClipboardContentKind.Image,
                    extension,
                    Translate($"clipboard.image-format.{extension.TrimStart('.')}", formatName),
                    Translate("clipboard.contains-image-extension", "The clipboard contains an image. It will be saved as {0}.", extension),
                    imagePreview: previewImage,
                    binaryPayload: rawImageBytes);
            }

            return new ClipboardContentSnapshot(
                ClipboardContentKind.Image,
                ".png",
                Translate("clipboard.image-format.png", "PNG image"),
                Translate("clipboard.contains-image-png", "The clipboard contains an image. It will be saved as .png."),
                imagePreview: previewImage);
        }

        if (Clipboard.ContainsText())
        {
            return new ClipboardContentSnapshot(
                ClipboardContentKind.Text,
                ".txt",
                Translate("clipboard.display.text-file", "Text file"),
                Translate("clipboard.contains-text", "The clipboard contains text. It will be saved as .txt."),
                textPreview: Clipboard.GetText());
        }

        if (Clipboard.ContainsAudio())
        {
            using var audioStream = Clipboard.GetAudioStream();
            if (audioStream is null)
            {
                throw new InvalidOperationException(Translate("clipboard.audio-read-failed", "The clipboard audio could not be read."));
            }

            using var copy = new MemoryStream();
            audioStream.CopyTo(copy);
            var audioBytes = copy.ToArray();

            return new ClipboardContentSnapshot(
                ClipboardContentKind.Audio,
                ".wav",
                Translate("clipboard.display.wav-audio", "WAV audio"),
                Translate("clipboard.contains-audio", "The clipboard contains audio. It will be saved as .wav."),
                binaryPayload: audioBytes);
        }

        if (dataObject is null)
        {
            throw new InvalidOperationException(Translate("clipboard.empty-or-unreadable", "The clipboard is empty or could not be read."));
        }

        if (TryExtractBinaryPayload(dataObject, out var bytes))
        {
            return new ClipboardContentSnapshot(
                ClipboardContentKind.Binary,
                ".bin",
                Translate("clipboard.display.binary-file", "Binary file"),
                Translate("clipboard.contains-binary", "The clipboard contains binary data. It will be saved as .bin."),
                binaryPayload: bytes);
        }

        throw new InvalidOperationException(Translate("clipboard.unsupported-format", "Could not extract a supported format from the clipboard."));
    }

    public static SaveResult SaveClipboardContent(string targetDirectory, string requestedName, ClipboardContentSnapshot snapshot)
    {
        Directory.CreateDirectory(targetDirectory);

        return snapshot.Kind switch
        {
            ClipboardContentKind.Archive => SaveFileDropList(targetDirectory, requestedName, snapshot.FileItems),
            ClipboardContentKind.Image => SaveImage(targetDirectory, requestedName, snapshot.ImagePreview, snapshot.Extension, snapshot.BinaryPayload),
            ClipboardContentKind.Text => SaveText(targetDirectory, requestedName, snapshot.TextPreview ?? string.Empty),
            ClipboardContentKind.Audio => SaveAudio(targetDirectory, requestedName, snapshot.BinaryPayload),
            ClipboardContentKind.Binary => SaveBinary(targetDirectory, requestedName, snapshot.BinaryPayload),
            _ => throw new InvalidOperationException(Translate("clipboard.unsupported-content", "Unsupported clipboard content."))
        };
    }

    private static SaveResult SaveText(string targetDirectory, string requestedName, string text)
    {
        var path = BuildPath(targetDirectory, requestedName, ".txt");
        File.WriteAllText(path, text);
        return new SaveResult(path, "text");
    }

    private static SaveResult SaveImage(string targetDirectory, string requestedName, Image? image, string extension, byte[]? rawBytes)
    {
        if (image is null)
        {
            throw new InvalidOperationException(Translate("clipboard.image-read-failed", "The clipboard image could not be read."));
        }

        if (rawBytes is { Length: > 0 })
        {
            var rawPath = BuildPath(targetDirectory, requestedName, extension);
            File.WriteAllBytes(rawPath, rawBytes);
            return new SaveResult(rawPath, "image");
        }

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
        {
            var jpegPath = BuildPath(targetDirectory, requestedName, extension);
            image.Save(jpegPath, ImageFormat.Jpeg);
            return new SaveResult(jpegPath, "image");
        }

        var pngPath = BuildPath(targetDirectory, requestedName, ".png");
        image.Save(pngPath, ImageFormat.Png);
        return new SaveResult(pngPath, "image");
    }

    private static SaveResult SaveFileDropList(string targetDirectory, string requestedName, IReadOnlyCollection<string>? items)
    {
        if (items is null || items.Count == 0)
        {
            throw new InvalidOperationException(Translate("clipboard.file-list-empty", "The clipboard file list is empty."));
        }

        var zipPath = BuildPath(targetDirectory, requestedName, ".zip");

        using var fileStream = File.Create(zipPath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        foreach (var rawItem in items)
        {
            var item = rawItem;
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            if (File.Exists(item))
            {
                archive.CreateEntryFromFile(item, Path.GetFileName(item), CompressionLevel.Optimal);
                continue;
            }

            if (Directory.Exists(item))
            {
                AddDirectoryToArchive(archive, item, Path.GetFileName(item));
            }
        }

        return new SaveResult(zipPath, "zip");
    }

    private static SaveResult SaveAudio(string targetDirectory, string requestedName, byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException(Translate("clipboard.audio-empty", "The clipboard audio payload is empty."));
        }

        var path = BuildPath(targetDirectory, requestedName, ".wav");
        File.WriteAllBytes(path, bytes);
        return new SaveResult(path, "audio");
    }

    private static void AddDirectoryToArchive(ZipArchive archive, string sourceDirectory, string rootName)
    {
        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, filePath);
            var entryPath = Path.Combine(rootName, relativePath).Replace('\\', '/');
            archive.CreateEntryFromFile(filePath, entryPath, CompressionLevel.Optimal);
        }

        if (!Directory.EnumerateFileSystemEntries(sourceDirectory).Any())
        {
            archive.CreateEntry($"{rootName}/");
        }
    }

    private static SaveResult SaveBinary(string targetDirectory, string requestedName, byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException(Translate("clipboard.binary-empty", "The clipboard binary payload is empty."));
        }

        var path = BuildPath(targetDirectory, requestedName, ".bin");
        File.WriteAllBytes(path, bytes);
        return new SaveResult(path, "binary");
    }

    private static bool TryGetRawImageBytes(IDataObject dataObject, out string extension, out byte[] bytes)
    {
        foreach (var candidate in new[]
        {
            new { Name = "PNG", Extension = ".png" },
            new { Name = "image/png", Extension = ".png" },
            new { Name = "JFIF", Extension = ".jpg" },
            new { Name = "JPEG", Extension = ".jpg" },
            new { Name = "image/jpeg", Extension = ".jpg" }
        })
        {
            if (!dataObject.GetDataPresent(candidate.Name))
            {
                continue;
            }

            var rawData = dataObject.GetData(candidate.Name);
            if (TryConvertToBytes(rawData, out bytes))
            {
                extension = candidate.Extension;
                return true;
            }
        }

        extension = ".png";
        bytes = Array.Empty<byte>();
        return false;
    }

    private static bool TryExtractBinaryPayload(IDataObject dataObject, out byte[] bytes)
    {
        foreach (var format in dataObject.GetFormats())
        {
            var value = dataObject.GetData(format);
            if (TryConvertToBytes(value, out bytes))
            {
                return true;
            }
        }

        bytes = Array.Empty<byte>();
        return false;
    }

    private static bool TryConvertToBytes(object? value, out byte[] bytes)
    {
        switch (value)
        {
            case byte[] rawBytes:
                bytes = rawBytes;
                return true;
            case MemoryStream memoryStream:
                bytes = memoryStream.ToArray();
                return true;
            case Stream stream:
                using (stream)
                using (var copy = new MemoryStream())
                {
                    stream.CopyTo(copy);
                    bytes = copy.ToArray();
                    return true;
                }
            default:
                bytes = Array.Empty<byte>();
                return false;
        }
    }

    private static string BuildPath(string targetDirectory, string requestedName, string extension)
    {
        var safeName = FileNameSanitizer.Sanitize(requestedName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = DefaultNames.Create();
        }

        safeName = Path.GetFileNameWithoutExtension(safeName);
        return Path.Combine(targetDirectory, safeName + extension);
    }
}

internal sealed record SaveResult(string FullPath, string Kind);

internal enum ClipboardContentKind
{
    Text,
    Image,
    Archive,
    Audio,
    Binary
}

internal sealed class ClipboardContentSnapshot : IDisposable
{
    public ClipboardContentSnapshot(
        ClipboardContentKind kind,
        string extension,
        string displayName,
        string promptMessage,
        string? textPreview = null,
        Image? imagePreview = null,
        IReadOnlyList<string>? fileItems = null,
        byte[]? binaryPayload = null)
    {
        Kind = kind;
        Extension = extension;
        DisplayName = displayName;
        PromptMessage = promptMessage;
        TextPreview = textPreview;
        ImagePreview = imagePreview;
        FileItems = fileItems ?? Array.Empty<string>();
        BinaryPayload = binaryPayload;
    }

    public ClipboardContentKind Kind { get; }
    public string Extension { get; }
    public string DisplayName { get; }
    public string PromptMessage { get; }
    public string? TextPreview { get; }
    public Image? ImagePreview { get; }
    public IReadOnlyList<string> FileItems { get; }
    public byte[]? BinaryPayload { get; }

    public void Dispose()
    {
        ImagePreview?.Dispose();
    }
}
