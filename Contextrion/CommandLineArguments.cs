namespace Contextrion;

internal enum AppMode
{
    Ui,
    Paste,
    Install,
    Uninstall,
    PickFolder,
    RestoreFolder,
    NewTimestampFolder,
    FriendlyName,
    ToDataUrl,
    CopyPath,
    EnumRename,
    CombineVertical,
    CombineHorizontal,
    CleanEmpty,
    CopyContent,
    Grayscale,
    Watermark,
    Crop,
    Circle,
    Resize,
    InvertColor,
    CleanMetadata,
    Minify,
    Optimize
}

internal sealed record CommandLineArguments(AppMode Mode, string? TargetPath, IReadOnlyList<string> TargetPaths)
{
    public static CommandLineArguments Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new(AppMode.Ui, null, Array.Empty<string>());
        }

        if (args[0].Equals("--paste", StringComparison.OrdinalIgnoreCase))
        {
            return new(AppMode.Paste, args.Length > 1 ? args[1] : null, Array.Empty<string>());
        }

        if (args[0].Equals("--install", StringComparison.OrdinalIgnoreCase))
        {
            return new(AppMode.Install, null, Array.Empty<string>());
        }

        if (args[0].Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
        {
            return new(AppMode.Uninstall, null, Array.Empty<string>());
        }

        if (args[0].Equals("--pick", StringComparison.OrdinalIgnoreCase))
        {
            return new(AppMode.PickFolder, ReadFolderArgument(args), Array.Empty<string>());
        }

        if (args[0].Equals("--restore-folder", StringComparison.OrdinalIgnoreCase))
        {
            return new(AppMode.RestoreFolder, ReadFolderArgument(args), Array.Empty<string>());
        }

        if (args[0].Equals("--new-timestamp-folder", StringComparison.OrdinalIgnoreCase))
        {
            return new(AppMode.NewTimestampFolder, ReadFolderArgument(args), Array.Empty<string>());
        }

        if (TryParseLegacyMode(args[0], out var mode))
        {
            return new(mode, null, ReadTargetArguments(args, 1));
        }

        return new(AppMode.Ui, null, Array.Empty<string>());
    }

    private static string? ReadFolderArgument(string[] args)
    {
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index].Equals("--folder", StringComparison.OrdinalIgnoreCase))
            {
                return index + 1 < args.Length ? args[index + 1] : null;
            }
        }

        return args.Length > 1 ? args[1] : null;
    }

    private static IReadOnlyList<string> ReadTargetArguments(string[] args, int startIndex)
    {
        if (args.Length <= startIndex)
        {
            return Array.Empty<string>();
        }

        return args
            .Skip(startIndex)
            .SelectMany(ExpandTargetArgument)
            .ToArray();
    }

    private static IEnumerable<string> ExpandTargetArgument(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        var trimmedPath = path.Trim();
        if (trimmedPath.Length == 0)
        {
            yield break;
        }

        if (File.Exists(trimmedPath) || Directory.Exists(trimmedPath))
        {
            yield return trimmedPath;
            yield break;
        }

        if (!trimmedPath.Contains('"'))
        {
            yield return trimmedPath;
            yield break;
        }

        var splitPaths = NativeMethods.SplitCommandLineArguments(trimmedPath);
        if (splitPaths.Length == 0)
        {
            yield return trimmedPath;
            yield break;
        }

        foreach (var splitPath in splitPaths.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            yield return splitPath;
        }
    }

    private static bool TryParseLegacyMode(string arg, out AppMode mode)
    {
        switch (arg.ToLowerInvariant())
        {
            case "--friendly-name":
                mode = AppMode.FriendlyName;
                return true;
            case "--tobase64":
                mode = AppMode.ToDataUrl;
                return true;
            case "--copy-path":
                mode = AppMode.CopyPath;
                return true;
            case "--enum":
                mode = AppMode.EnumRename;
                return true;
            case "--combine-vertical":
                mode = AppMode.CombineVertical;
                return true;
            case "--combine-horizontal":
                mode = AppMode.CombineHorizontal;
                return true;
            case "--clean-empty":
                mode = AppMode.CleanEmpty;
                return true;
            case "--copy-content":
                mode = AppMode.CopyContent;
                return true;
            case "--grayscale":
                mode = AppMode.Grayscale;
                return true;
            case "--watermark":
                mode = AppMode.Watermark;
                return true;
            case "--crop":
                mode = AppMode.Crop;
                return true;
            case "--circle":
                mode = AppMode.Circle;
                return true;
            case "--resize":
                mode = AppMode.Resize;
                return true;
            case "--invert-color":
                mode = AppMode.InvertColor;
                return true;
            case "--clean-metadata":
                mode = AppMode.CleanMetadata;
                return true;
            case "--minify":
                mode = AppMode.Minify;
                return true;
            case "--optimize":
                mode = AppMode.Optimize;
                return true;
            default:
                mode = AppMode.Ui;
                return false;
        }
    }
}
