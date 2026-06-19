using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VstDeleter.Models;
using VstDeleter.Helpers;
using VstDeleter.Services;

namespace VstDeleter.ViewModels;

public enum AppPhase
{
    Search,
    Scanning,
    Results,
    BackingUp,
    Deleting,
    Done,
    RestoreSelect,
    RestoreReview,
    Restoring,
    RestoreDone
}

public partial class MainViewModel : ViewModelBase, IDisposable
{
    public MainViewModel()
    {
        // Załaduj zainstalowane wtyczki przy starcie
        _ = LoadLocalPluginsAsync();

        // Subskrypcja logów systemowych
        Services.AppLogger.OnLog += OnLogReceived;
    }

    private void OnLogReceived(string message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            AppLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        });
    }
    // ── Dostęp do Storage (file/folder picker) ────────────────────────────────
    private TopLevel? _topLevel;
    public void SetTopLevel(TopLevel tl) => _topLevel = tl;

    // ── Faza aplikacji ────────────────────────────────────────────────────────
    [ObservableProperty] private AppPhase _phase = AppPhase.Search;

    // ── Wyszukiwarka ─────────────────────────────────────────────────────────
    [ObservableProperty] private string  _searchText = string.Empty;
    [ObservableProperty] private bool    _showSuggestions;
    [ObservableProperty] private string? _selectedPlugin;

    public ObservableCollection<string> Suggestions { get; } = new();

    // ── Zlokalizowane wtyczki (Discovery) ────────────────────────────────────
    public ObservableCollection<InstalledPlugin> LocalPlugins { get; } = new();

    // Dziennik zdarzeń diagnostycznych (Logi)
    public ObservableCollection<string> AppLogs { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedLocalPluginFormatsText))]
    private InstalledPlugin? _selectedLocalPlugin;

    public string SelectedLocalPluginFormatsText =>
        SelectedLocalPlugin == null 
            ? string.Empty 
            : LanguageService.Instance["Phase1_DetectedFormats"] + " " + string.Join(", ", SelectedLocalPlugin.Formats);

    [RelayCommand]
    private async Task LoadLocalPluginsAsync()
    {
        try
        {
            var list = await InstalledPluginsDiscovery.DiscoverPluginsAsync();
            LocalPlugins.Clear();
            foreach (var p in list)
                LocalPlugins.Add(p);
        }
        catch (Exception ex)
        {
            AppLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [LoadLocalPluginsAsync] {LanguageService.Instance["Log_FatalError"]}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ScanLocalPluginAsync()
    {
        if (SelectedLocalPlugin == null) return;
        
        // Bezszwowa integracja z obecnym pipeline'm skanującym
        SearchText = SelectedLocalPlugin.Name;
        await StartScanAsync();
    }

    // ── Skanowanie ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _scanStatus = string.Empty;

    // ── Wyniki ────────────────────────────────────────────────────────────────
    public ObservableCollection<FoundItem> FoundItems { get; } = new();

    [ObservableProperty] private string _totalSizeLabel   = string.Empty;
    [ObservableProperty] private string _selectedSizeLabel = string.Empty;
    [ObservableProperty] private bool   _hasResults;
    [ObservableProperty] private bool   _allSelected = true;

    // ── Backup ────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyPropertyChangedFor(nameof(BackupSizeInfo))]
    private bool _backupEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyPropertyChangedFor(nameof(BackupSizeInfo))]
    private string _backupFolderPath = string.Empty;

    [ObservableProperty] private string _backupProgressText = string.Empty;
    [ObservableProperty] private string _createdManifestPath = string.Empty;

    public bool CanDelete =>
        !BackupEnabled || !string.IsNullOrWhiteSpace(BackupFolderPath);

    public string BackupSizeInfo
    {
        get
        {
            if (!BackupEnabled) return string.Empty;
            long size    = FoundItems.Where(i => i.IsSelected).Sum(i => i.SizeBytes);
            string sizeS = FormatSize(size);
            string folder = string.IsNullOrWhiteSpace(BackupFolderPath) ? LanguageService.Instance["Backup_NoFolder"] : BackupFolderPath;
            return string.Format(LanguageService.Instance["Backup_EstimatedSize"], sizeS, folder);
        }
    }

    // ── Usuwanie / wynik ──────────────────────────────────────────────────────
    [ObservableProperty] private string _deleteStatus   = string.Empty;
    [ObservableProperty] private string _deleteProgress = string.Empty;
    [ObservableProperty] private bool   _hasErrors;
    [ObservableProperty] private string _errorDetails   = string.Empty;
    [ObservableProperty] private int    _deletedCount;

    // ── Przywracanie ──────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RestoreEntries))]
    [NotifyPropertyChangedFor(nameof(RestoreHeaderInfo))]
    [NotifyPropertyChangedFor(nameof(HasLoadedManifest))]
    private BackupManifest? _loadedManifest;

    [ObservableProperty] private string _manifestFilePath   = string.Empty;
    [ObservableProperty] private string _restoreStatus      = string.Empty;
    [ObservableProperty] private string _restoreProgressText = string.Empty;
    [ObservableProperty] private bool   _restoreHasErrors;
    [ObservableProperty] private string _restoreErrorDetails = string.Empty;
    [ObservableProperty] private int    _restoredCount;

    public IReadOnlyList<BackupEntry> RestoreEntries =>
        LoadedManifest?.Entries ?? (IReadOnlyList<BackupEntry>)Array.Empty<BackupEntry>();

    public bool HasLoadedManifest => LoadedManifest != null;

    public string RestoreHeaderInfo => LoadedManifest == null ? string.Empty :
        string.Format(LanguageService.Instance["Restore_HeaderInfo"], LoadedManifest.PluginName, LoadedManifest.Created, LoadedManifest.Entries.Count);

    public string CurrentLanguageTarget => LanguageService.Instance.CurrentLanguage == "pl" ? "EN" : "PL";

    [RelayCommand]
    private void ToggleLanguage()
    {
        LanguageService.Instance.ToggleLanguage();
        OnPropertyChanged(nameof(CurrentLanguageTarget));
        OnPropertyChanged(nameof(SelectedLocalPluginFormatsText));
        
        foreach (var item in FoundItems)
        {
            item.RefreshTranslation();
        }
        
        if (LoadedManifest != null)
        {
            foreach (var entry in LoadedManifest.Entries)
            {
                entry.RefreshTranslation();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────
    private CancellationTokenSource? _cts;
    private bool _isBulkUpdating;

    public void Dispose()
    {
        Services.AppLogger.OnLog -= OnLogReceived;
        DetachFoundItemHandlers();
        if (_cts != null) { try { _cts.Cancel(); _cts.Dispose(); } catch { } _cts = null; }
        GC.SuppressFinalize(this);
    }

    private void OnFoundItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isBulkUpdating) return;
        RecalcSelectedSize();
        OnPropertyChanged(nameof(BackupSizeInfo));
    }

    private void DetachFoundItemHandlers()
    {
        foreach (var item in FoundItems)
            item.PropertyChanged -= OnFoundItemPropertyChanged;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  AUTOUZUPEŁNIANIE
    // ═════════════════════════════════════════════════════════════════════════
    partial void OnSearchTextChanged(string value)
    {
        Suggestions.Clear();
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2) { ShowSuggestions = false; return; }

        foreach (string s in PluginDatabase.Search(value, maxResults: 8))
            Suggestions.Add(s);

        ShowSuggestions = Suggestions.Count > 0;
    }

    [RelayCommand]
    private void SelectSuggestion(string suggestion)
    {
        SearchText      = suggestion;
        SelectedPlugin  = suggestion;
        ShowSuggestions = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SKANOWANIE
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task StartScanAsync()
    {
        string plugin = string.IsNullOrWhiteSpace(SelectedPlugin)
            ? SearchText.Trim()
            : SelectedPlugin;

        if (string.IsNullOrWhiteSpace(plugin)) return;

        SelectedPlugin  = plugin;
        ShowSuggestions = false;
        Phase           = AppPhase.Scanning;
        ScanStatus      = $"Szukam śladów: {plugin}…";
        DetachFoundItemHandlers();
        FoundItems.Clear();
        AppLogs.Clear();

        if (_cts != null) { try { _cts.Cancel(); _cts.Dispose(); } catch { } }
        _cts = new CancellationTokenSource();

        var currentToken = _cts.Token;

        using var progress = new ThrottledProgress<string>(new Progress<string>(p =>
            ScanStatus = $"Sprawdzam: {ShortenPath(p)}"), TimeSpan.FromMilliseconds(50));

        try
        {
            var items = await PluginScanner.ScanAsync(plugin, progress, currentToken);

            foreach (var item in items)
            {
                item.PropertyChanged += OnFoundItemPropertyChanged;
                FoundItems.Add(item);
            }

            HasResults = FoundItems.Count > 0;
            Phase      = AppPhase.Results;
            RecalcSelectedSize();
            RecalcTotalSize();
        }
        catch (ArgumentException ex)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.Search;
                ScanStatus = $"⚠ Błąd zapytania: {ex.Message}";
            }
        }
        catch (OperationCanceledException)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.Search;
            }
        }
        catch (Exception ex)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.Search;
                ScanStatus = $"⚠ Błąd krytyczny: {ex.Message}";
                AppLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [StartScanAsync] {ex}");
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ZAZNACZANIE
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void SelectAll()
    {
        _isBulkUpdating = true;
        try
        {
            foreach (var item in FoundItems) item.IsSelected = true;
        }
        finally
        {
            _isBulkUpdating = false;
        }
        AllSelected = true;
        RecalcSelectedSize();
        OnPropertyChanged(nameof(BackupSizeInfo));
    }

    [RelayCommand]
    private void DeselectAll()
    {
        _isBulkUpdating = true;
        try
        {
            foreach (var item in FoundItems) item.IsSelected = false;
        }
        finally
        {
            _isBulkUpdating = false;
        }
        AllSelected = false;
        RecalcSelectedSize();
        OnPropertyChanged(nameof(BackupSizeInfo));
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  BACKUP — wybór folderu
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task ChooseBackupFolderAsync()
    {
        if (_topLevel?.StorageProvider is not { } sp) return;

        var folders = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title       = "Wybierz folder na kopię zapasową",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            BackupFolderPath = folders[0].Path.LocalPath;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  TYLKO KOPIA ZAPASOWA (bez usuwania)
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task BackupOnlyAsync()
    {
        if (string.IsNullOrWhiteSpace(BackupFolderPath)) return;

        var toBackup = FoundItems.Where(i => i.IsSelected).ToList();
        if (toBackup.Count == 0) return;

        if (_cts != null) { try { _cts.Cancel(); _cts.Dispose(); } catch { } }
        _cts = new CancellationTokenSource();

        var currentToken = _cts.Token;

        Phase              = AppPhase.BackingUp;
        BackupProgressText = LanguageService.Instance["Backup_Preparing"];
        CreatedManifestPath = string.Empty;

        int done = 0;
        using var progress = new ThrottledProgress<string>(new Progress<string>(p =>
        {
            done++;
            BackupProgressText = $"[{done}/{toBackup.Count}]  {p}";
        }), TimeSpan.FromMilliseconds(50));

        try
        {
            CreatedManifestPath = await BackupService.CreateBackupAsync(
                SelectedPlugin!, toBackup, BackupFolderPath, progress, currentToken);
        }
        catch (OperationCanceledException)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.Results;
            }
            return;
        }
        catch (Exception ex)
        {
            CreatedManifestPath = string.Empty;
            BackupProgressText  = $"Błąd: {ex.Message}";
            try { await Task.Delay(2000, currentToken); } catch (OperationCanceledException) { }
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.Results;
            }
            return;
        }

        // Wróć do wyników — pliki NIE zostały usunięte
        Phase        = AppPhase.Done;
        HasErrors    = false;
        ErrorDetails = string.Empty;
        DeletedCount = 0;
        DeleteStatus = $"✓ Kopia zapasowa zapisana. Żaden plik nie został usunięty.";
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  USUWANIE (z opcjonalnym backupem)
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var toDelete = FoundItems.Where(i => i.IsSelected).ToList();
        if (toDelete.Count == 0) return;

        if (_cts != null) { try { _cts.Cancel(); _cts.Dispose(); } catch { } }
        _cts = new CancellationTokenSource();

        var currentToken = _cts.Token;

        // ── Krok 1: Backup (opcjonalnie) ─────────────────────────────────────
        if (BackupEnabled)
        {
            if (string.IsNullOrWhiteSpace(BackupFolderPath))
            {
                HasErrors = true;
                ErrorDetails = LanguageService.Instance["Delete_FolderRequired"];
                Phase = AppPhase.Done;
                DeleteStatus = LanguageService.Instance["Delete_AbortedSafe"];
                return;
            }

            Phase             = AppPhase.BackingUp;
            BackupProgressText = LanguageService.Instance["Backup_Preparing"];
            CreatedManifestPath = string.Empty;

            int done = 0;
            using var backupProgress = new ThrottledProgress<string>(new Progress<string>(p =>
            {
                done++;
                BackupProgressText = $"[{done}/{toDelete.Count}]  {p}";
            }), TimeSpan.FromMilliseconds(50));

            try
            {
                CreatedManifestPath = await BackupService.CreateBackupAsync(
                    SelectedPlugin!, toDelete, BackupFolderPath, backupProgress, currentToken);
            }
            catch (OperationCanceledException)
            {
                if (_cts != null && _cts.Token == currentToken)
                {
                    Phase = AppPhase.Results;
                }
                return;
            }
            catch (Exception ex)
            {
                CreatedManifestPath = string.Empty;
                BackupProgressText  = $"Błąd kopii: {ex.Message}";
                try { await Task.Delay(2000, currentToken); } catch (OperationCanceledException) { }
                if (_cts != null && _cts.Token == currentToken)
                {
                    Phase = AppPhase.Results;
                }
                return;
            }
        }

        // ── Krok 2: Usuwanie ──────────────────────────────────────────────────
        Phase        = AppPhase.Deleting;
        DeleteStatus = "Usuwam zaznaczone elementy…";
        HasErrors    = false;
        ErrorDetails = string.Empty;

        int delDone = 0;
        using var delProgress = new ThrottledProgress<string>(new Progress<string>(p =>
        {
            delDone++;
            DeleteProgress = $"[{delDone}/{toDelete.Count}]  {p}";
        }), TimeSpan.FromMilliseconds(50));

        try
        {
            var result = await PluginScanner.DeleteAsync(toDelete, delProgress, currentToken);

            DeletedCount = result.Deleted;
            HasErrors    = result.Errors > 0;
            ErrorDetails = result.Errors > 0
                ? string.Join("\n", result.ErrorMessages)
                : string.Empty;

            if (_cts != null && _cts.Token == currentToken)
            {
                Phase        = AppPhase.Done;
                DeleteStatus = result.Errors == 0
                    ? $"✓ Usunięto {result.Deleted} element(ów) pomyślnie."
                    : $"Usunięto {result.Deleted}, błędów: {result.Errors}.";
            }
        }
        catch (OperationCanceledException)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.Results;
            }
        }
        catch (Exception ex)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.Done;
                HasErrors = true;
                ErrorDetails = ex.Message;
                DeleteStatus = "⚠ Błąd krytyczny podczas usuwania.";
                AppLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [DeleteSelectedAsync] {ex}");
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRZYWRACANIE — nawigacja i wybór manifestu
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void GoToRestore()
    {
        Phase           = AppPhase.RestoreSelect;
        ManifestFilePath = string.Empty;
        LoadedManifest  = null;
    }

    [RelayCommand]
    private async Task ChooseManifestFileAsync()
    {
        if (_topLevel?.StorageProvider is not { } sp) return;

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = "Wybierz plik manifestu kopii (VSTDeleter_manifest.json)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Manifest VST Deleter")
                    { Patterns = new[] { "VSTDeleter_manifest.json", "*.json" } },
                new FilePickerFileType("Wszystkie pliki")
                    { Patterns = new[] { "*" } }
            }
        });

        if (files.Count == 0) return;

        ManifestFilePath = files[0].Path.LocalPath;
        TryLoadManifest();
    }

    /// <summary>
    /// Ładuje manifest z bieżącej ManifestFilePath i przechodzi do ekranu podglądu.
    /// </summary>
    [RelayCommand]
    private void LoadManifestAndReview()
    {
        TryLoadManifest();
        if (HasLoadedManifest)
            Phase = AppPhase.RestoreReview;
    }

    private void TryLoadManifest()
    {
        if (string.IsNullOrWhiteSpace(ManifestFilePath)) return;
        LoadedManifest = BackupService.LoadManifest(ManifestFilePath);

        if (LoadedManifest == null)
            ManifestFilePath = "⚠ Nie można wczytać pliku — sprawdź czy to poprawny manifest VST Deleter.";
    }

    /// <summary>Ładuje manifest na podstawie ścieżki manifestu z właśnie zakończonego backupu.</summary>
    [RelayCommand]
    private void LoadCreatedManifestAndReview()
    {
        if (string.IsNullOrWhiteSpace(CreatedManifestPath)) return;
        ManifestFilePath = CreatedManifestPath;
        LoadedManifest   = BackupService.LoadManifest(CreatedManifestPath);
        if (HasLoadedManifest)
            Phase = AppPhase.RestoreReview;
    }

    [RelayCommand]
    private void BackToRestoreSelect()
    {
        Phase = AppPhase.RestoreSelect;
    }

    [RelayCommand]
    private void BackToSearchFromRestore()
    {
        Phase            = AppPhase.Search;
        LoadedManifest   = null;
        ManifestFilePath = string.Empty;
        RestoreStatus    = string.Empty;
        RestoreErrorDetails = string.Empty;
        RestoreHasErrors = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRZYWRACANIE — właściwe
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task StartRestoreAsync()
    {
        if (LoadedManifest == null) return;

        Phase              = AppPhase.Restoring;
        RestoreStatus      = "Przywracam pliki...";
        RestoreHasErrors   = false;
        RestoreErrorDetails = string.Empty;
        AppLogs.Clear();

        if (_cts != null) { try { _cts.Cancel(); _cts.Dispose(); } catch { } }
        _cts = new CancellationTokenSource();

        var currentToken = _cts.Token;

        int done = 0;
        int total = LoadedManifest.Entries.Count;
        using var progress = new ThrottledProgress<string>(new Progress<string>(p =>
        {
            done++;
            RestoreProgressText = $"[{done}/{total}]  {p}";
        }), TimeSpan.FromMilliseconds(50));

        try
        {
            var result = await BackupService.RestoreBackupAsync(
                LoadedManifest, progress, currentToken);

            RestoredCount       = result.Restored;
            
            if (result.Errors == 0)
            {
                if (result.Skipped > 0)
                {
                    RestoreStatus = $"✓ Przywrócono {result.Restored} plików. (Pominięto {result.Skipped} paragonów SIP, nie wpływa na wtyczki)";
                }
                else
                {
                    RestoreStatus = $"✓ Przywrócono {result.Restored} element(ów) pomyślnie.";
                }
            }
            else
            {
                RestoreHasErrors = true;
                RestoreStatus = $"Przywrócono {result.Restored}, błędów: {result.Errors}, pominięto: {result.Skipped}.";
                RestoreErrorDetails = string.Join("\n", result.ErrorMessages);
            }

            if (_cts != null && _cts.Token == currentToken)
            {
                Phase         = AppPhase.RestoreDone;
            }
        }
        catch (OperationCanceledException)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.RestoreSelect;
            }
        }
        catch (Exception ex)
        {
            if (_cts != null && _cts.Token == currentToken)
            {
                Phase = AppPhase.RestoreSelect;
                ManifestFilePath = $"⚠ Błąd krytyczny przywracania: {ex.Message}";
                AppLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [StartRestoreAsync] {ex}");
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  RESET / NAWIGACJA
    // ═════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void Reset()
    {
        if (_cts != null) { try { _cts.Cancel(); _cts.Dispose(); } catch { } _cts = null; }
        Phase              = AppPhase.Search;
        SearchText         = string.Empty;
        SelectedPlugin     = null;
        ShowSuggestions    = false;
        DetachFoundItemHandlers();
        FoundItems.Clear();
        AppLogs.Clear();
        HasResults         = false;
        HasErrors          = false;
        ErrorDetails       = string.Empty;
        DeleteStatus       = string.Empty;
        ScanStatus         = string.Empty;
        TotalSizeLabel     = string.Empty;
        SelectedSizeLabel  = string.Empty;
        BackupEnabled      = false;
        BackupFolderPath   = string.Empty;
        BackupProgressText = string.Empty;
        CreatedManifestPath = string.Empty;
        LoadedManifest     = null;
        ManifestFilePath   = string.Empty;
        RestoreStatus      = string.Empty;
        RestoreErrorDetails = string.Empty;
        RestoreHasErrors   = false;
    }

    [RelayCommand]
    private void BackToResults()
    {
        Phase = AppPhase.Results;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════════
    private void RecalcTotalSize()
    {
        long total = FoundItems.Sum(i => i.SizeBytes);
        TotalSizeLabel = FormatSize(total);
    }

    private void RecalcSelectedSize()
    {
        long sel = FoundItems.Where(i => i.IsSelected).Sum(i => i.SizeBytes);
        SelectedSizeLabel = FormatSize(sel);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)                  return $"{bytes} B";
        if (bytes < 1024 * 1024)          return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024)  return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static string ShortenPath(string path)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string s = path.StartsWith(home) ? "~" + path[home.Length..] : path;
        return s.Length > 55 ? "…" + s[^52..] : s;
    }
}
