namespace Contextrion;

internal static class AppIconProvider
{
    private static Icon? _cachedIcon;

    public static Icon GetIcon()
    {
        if (_cachedIcon is not null)
        {
            return _cachedIcon;
        }

        using var stream = BundledAssetCatalog.OpenClipboardIconStream();
        using var icon = new Icon(stream);
        _cachedIcon = (Icon)icon.Clone();
        return _cachedIcon;
    }

    public static string GetIconPath()
    {
        return BundledAssetCatalog.EnsureClipboardIconFile(AppContext.BaseDirectory);
    }

    public static string GetFolderIconPath()
    {
        return BundledAssetCatalog.EnsureFolderizeIconFile(AppContext.BaseDirectory);
    }
}
