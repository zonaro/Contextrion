using System.Globalization;

namespace Contextrion;

internal static class AppLocalization
{
    private static readonly Lazy<JsonLocalizationDictionary> Current = new(LoadDictionary);

    public static string Translate(string key, string defaultString, params string[] args)
    {
        return Current.Value.Translate(key, defaultString, args);
    }

    private static JsonLocalizationDictionary LoadDictionary()
    {
        var culture = CultureInfo.CurrentUICulture;
        foreach (var filePath in GetCandidateFilePaths(culture))
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            return JsonLocalizationDictionary.LoadFromFile(filePath, culture);
        }

        return JsonLocalizationDictionary.Empty(culture);
    }

    private static IEnumerable<string> GetCandidateFilePaths(CultureInfo culture)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Localization");

        for (var current = culture; current != CultureInfo.InvariantCulture; current = current.Parent)
        {
            if (string.IsNullOrWhiteSpace(current.Name))
            {
                break;
            }

            yield return Path.Combine(directory, $"{current.Name}.json");
        }

        yield return Path.Combine(directory, "default.json");
    }
}
