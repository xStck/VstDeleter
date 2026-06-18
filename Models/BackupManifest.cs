using System.Text.Json.Serialization;

namespace VstDeleter.Models;

/// <summary>
/// Manifest kopii zapasowej — zapisywany jako czytelny JSON w folderze kopii.
/// Pozwala na ręczne przywrócenie plików bez użycia programu.
/// </summary>
public class BackupManifest
{
    [JsonPropertyName("_info")]
    public string Info { get; init; } =
        "Kopia zapasowa VST Deleter. " +
        "Aby przywrócić ręcznie: dla każdego wpisu skopiuj plik/katalog " +
        "z lokalizacji <backupFolder>/<backupRelativePath> " +
        "z powrotem do <originalAbsolutePath>.";

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = "1.0";

    [JsonPropertyName("pluginName")]
    public string PluginName { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; init; } = DateTimeOffset.Now;

    [JsonPropertyName("backupFolder")]
    public string BackupFolder { get; set; } = string.Empty;

    [JsonPropertyName("totalEntries")]
    public int TotalEntries => Entries.Count;

    [JsonPropertyName("entries")]
    public List<BackupEntry> Entries { get; init; } = new();
}

/// <summary>
/// Jeden wpis w manifeście — opisuje jeden plik lub katalog.
/// </summary>
public class BackupEntry
{
    /// <summary>Oryginalna, absolutna ścieżka na dysku (cel przywrócenia).</summary>
    [JsonPropertyName("originalAbsolutePath")]
    public string OriginalAbsolutePath { get; init; } = string.Empty;

    /// <summary>
    /// Ścieżka relatywna wewnątrz folderu kopii.
    /// Pełna lokalizacja kopii = backupFolder + "/" + backupRelativePath.
    /// </summary>
    [JsonPropertyName("backupRelativePath")]
    public string BackupRelativePath { get; init; } = string.Empty;

    /// <summary>"file" lub "directory".</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "file";

    /// <summary>Kategoria wykryta przez skaner (np. "VST3 (systemowy)").</summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    /// <summary>Rozmiar w bajtach.</summary>
    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; init; }

    // ── Pomocnicze (nie serializowane) ────────────────────────────────────────
    [JsonIgnore] public string TypeIcon => Type == "directory" ? "📁" : "📄";
    [JsonIgnore] public string SizeFormatted => FormatSize(SizeBytes);
    [JsonIgnore] public string ShortOriginalPath
    {
        get
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string p = OriginalAbsolutePath.StartsWith(home)
                ? "~" + OriginalAbsolutePath[home.Length..]
                : OriginalAbsolutePath;
            return p.Length > 60 ? "…" + p[^57..] : p;
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)                  return $"{bytes} B";
        if (bytes < 1024 * 1024)          return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024)  return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
