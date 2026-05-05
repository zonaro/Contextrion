namespace Contextrion;

internal enum FolderIconSourceKind
{
    BuiltIn,
    Pack,
    User
}

internal sealed record FolderIconEntry(
    string Label,
    string ResourcePath,
    int ResourceIndex,
    FolderIconSourceKind SourceKind,
    string Category,
    bool IsCurrentOsCategory = false);
