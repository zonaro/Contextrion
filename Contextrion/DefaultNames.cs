namespace Contextrion;

internal static class DefaultNames
{
    public static string Create()
    {
        return Translate(
            "defaults.clipboard-name",
            "Clipboard_{0}",
            DateTime.Now.ToString("yyyyMMdd_HHmmss"));
    }
}
