namespace VstDeleter.Helpers;

public static class BashHelpers
{
    /// <summary>
    /// Bezpiecznie eskejpuje ciąg znaków (np. ścieżkę), aby mógł zostać wstawiony
    /// pomiędzy pojedyncze cudzysłowy w skrypcie Bash.
    /// Zastępuje każdy apostrof (') sekwencją ('\'').
    /// </summary>
    public static string Escape(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("'", "'\\''");
    }
}
