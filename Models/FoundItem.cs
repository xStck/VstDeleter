using CommunityToolkit.Mvvm.ComponentModel;

namespace VstDeleter.Models;

public partial class FoundItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true;

    public string Path { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    public long SizeBytes { get; init; }

    public string SizeFormatted => FormatSize(SizeBytes);

    public string TypeLabel => IsDirectory ? "katalog" : "plik";
    public string TypeIcon  => IsDirectory ? "📁" : "📄";

    public string ShortPath
    {
        get
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.StartsWith(home)
                ? "~" + Path[home.Length..]
                : Path;
        }
    }

    // Category for grouping in the UI
    public string Category { get; init; } = "Inne";
    public string CategoryTranslated => Services.LanguageService.Instance[Category];

    public void RefreshTranslation()
    {
        OnPropertyChanged(nameof(CategoryTranslated));
        OnPropertyChanged(nameof(TypeLabel));
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)              return $"{bytes} B";
        if (bytes < 1024 * 1024)      return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
