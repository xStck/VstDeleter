using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace VstDeleter.Services;

public class LanguageService : INotifyPropertyChanged
{
    public static LanguageService Instance { get; } = new();

    private string _currentLanguage = "en";
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(nameof(CurrentLanguage));
                OnPropertyChanged("Item"); // Wymusza odświeżenie wszystkich bindowań indexera w XAML
            }
        }
    }

    public void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == "pl" ? "en" : "pl";
    }

    public string this[string key]
    {
        get
        {
            var dict = _currentLanguage == "en" ? _en : _pl;
            return dict.TryGetValue(key, out var val) ? val : key;
        }
    }

    private readonly Dictionary<string, string> _pl = new()
    {
        // ── MainWindow.axaml ────────────────────────────────────────────────
        { "AppTitle", "VST Deleter" },
        { "Phase1_Title", "Wyczyść wtyczki VST & AU" },
        { "Phase1_Subtitle", "Wyszukaj wtyczkę, przejrzyj pliki i usuń to, co chcesz." },
        { "Phase1_PluginNameLabel", "NAZWA WTYCZKI" },
        { "Phase1_SearchPlaceholder", "np. Guitar Rig 6, Serum, Kontakt…" },
        { "Phase1_ScanBtn", "Skanuj" },
        { "Phase1_LocalPluginsLabel", "ALBO WYBIERZ Z ZAINSTALOWANYCH" },
        { "Phase1_LocalPluginsPlaceholder", "Wybierz wykrytą wtyczkę z dysku..." },
        { "Phase1_LocalScanBtn", "Skanuj z systemu" },
        { "Phase1_DetectedFormats", "Wykryto formaty:" },
        { "Phase1_Hint", "💡  Baza zawiera 200+ znanych wtyczek VST/AU/CLAP. Możesz też wpisać własną nazwę." },
        { "Phase1_RestoreBoxTitle", "Masz już kopię zapasową?" },
        { "Phase1_RestoreBoxSub", "Wczytaj manifest i przywróć usunięte wcześniej pliki." },
        { "Phase1_RestoreBtn", "Przywróć kopię" },

        { "Phase2_Searching", "Szukam śladów po wtyczce…" },

        { "Phase3_ResultsFor", "Wyniki dla: {0}" },
        { "Phase3_FoundItemsCount", "Znaleziono {0} elementów · łącznie {1}" },
        { "Phase3_NewSearchBtn", "Nowe wyszukiwanie" },
        { "Phase3_SelectAll", "Zaznacz wszystko" },
        { "Phase3_DeselectAll", "Odznacz wszystko" },
        { "Phase3_SelectedLabel", "Zaznaczono: {0}" },
        { "Phase3_BackupCheckbox", "🔒  Utwórz kopię zapasową przed usunięciem" },
        { "Phase3_BackupSelectFolder", "(kliknij Wybierz…)" },
        { "Phase3_BackupChooseBtn", "Wybierz…" },
        { "Phase3_NoResultsTitle", "Nie znaleziono żadnych śladów" },
        { "Phase3_NoResultsSub", "System jest czysty. Ta wtyczka nie pozostawiła śladów." },
        { "Phase3_DeleteSummary", "Do usunięcia: {0}  ·  Rozmiar: {1}" },
        { "Phase3_CancelBtn", "Anuluj" },
        { "Phase3_BackupOnlyBtn", "Tylko kopia" },
        { "Phase3_BackupOnlyTooltip", "Wymaga wybranego folderu kopii zapasowej" },
        { "Phase3_DeleteSelectedBtn", "Usuń zaznaczone" },

        { "Phase4_Deleting", "Usuwam pliki…" },

        { "Phase5_BackupSaved", "🔒  Kopia zapasowa zapisana:" },
        { "Phase5_PreviewBtn", "Podgląd →" },
        { "Phase5_SystemFilesHint", "💡 Pliki systemowe wymagają uprawnień administratora (uruchom z sudo)." },
        { "Phase5_BackToResultsBtn", "← Wróć do wyników" },

        { "Phase5b_BackingUp", "Tworzę kopię zapasową…" },
        { "Phase5b_WaitHint", "Nie zamykaj programu — kopia jest tworzona…" },

        { "Phase6_RestoreTitle", "Przywróć kopię zapasową" },
        { "Phase6_RestoreSub", "Wybierz plik manifestu (VSTDeleter_manifest.json) z folderu kopii.\nManifest zawiera mapę wszystkich plików i ich oryginalnych lokalizacji." },
        { "Phase6_RestoreClickBelow", "Kliknij poniżej, aby wybrać manifest kopii" },
        { "Phase6_ChooseManifestBtn", "Wybierz VSTDeleter_manifest.json" },
        { "Phase6_ManifestLoaded", "✓  Manifest wczytany poprawnie:" },
        { "Phase6_BackBtn", "← Wróć" },
        { "Phase6_PreviewRestoreBtn", "Podgląd i przywracanie" },

        { "Phase7_ReviewTitle", "Podgląd przywracania" },
        { "Phase7_BackupFolderLabel", "Folder kopii:" },
        { "Phase7_RestoreSelectedBtn", "Przywróć {0} plików" },

        { "Phase8_Restoring", "Przywracam pliki..." },
        { "Phase9_SuccessTitle", "Zakończono!" },

        // ── MainViewModel.cs ────────────────────────────────────────────────
        { "Log_FatalError", "Błąd krytyczny" },
        { "Scan_Searching", "Szukam śladów:" },
        { "Scan_Checking", "Sprawdzam:" },
        { "Scan_QueryError", "⚠ Błąd zapytania:" },
        { "Backup_SelectFolderDialog", "Wybierz folder na kopię zapasową" },
        { "Backup_EstimatedSize", "Szacowana kopia: ~{0}  →  {1}" },
        { "Backup_NoFolder", "(wybierz folder)" },
        { "Backup_Preparing", "Przygotowuję kopię…" },
        { "Backup_Error", "Błąd:" },
        { "Backup_OnlySuccess", "✓ Kopia zapasowa zapisana. Żaden plik nie został usunięty." },
        { "Delete_FolderRequired", "Aby kontynuować, wybierz folder docelowy dla kopii zapasowej (lub odznacz tę opcję)." },
        { "Delete_AbortedSafe", "Przerwano operację ze względów bezpieczeństwa." },
        { "Delete_BackupError", "Błąd kopii:" },
        { "Delete_DeletingItems", "Usuwam zaznaczone elementy…" },
        { "Delete_Success", "✓ Usunięto {0} element(ów) pomyślnie." },
        { "Delete_Partial", "Usunięto {0}, błędów: {1}." },
        { "Manifest_SelectDialog", "Wybierz plik manifestu kopii (VSTDeleter_manifest.json)" },
        { "Manifest_Invalid", "⚠ Nie można wczytać pliku — sprawdź czy to poprawny manifest VST Deleter." },
        { "Restore_HeaderInfo", "Wtyczka: {0}  ·  Kopia z: {1:dd.MM.yyyy HH:mm}  ·  {2} elementów" },
        { "Restore_SuccessSkipped", "✓ Przywrócono {0} plików. (Pominięto {1} paragonów SIP, nie wpływa na wtyczki)" },
        { "Restore_Success", "✓ Przywrócono {0} element(ów) pomyślnie." },
        { "Restore_Partial", "Przywrócono {0}, błędów: {1}, pominięto: {2}." },

        // ── Usługi pod spodem ────────────────────────────────────────────────
        { "Cat_PluginAU", "Wtyczka (AudioUnit)" },
        { "Cat_PluginVST", "Wtyczka (VST)" },
        { "Cat_PluginVST3", "Wtyczka (VST3)" },
        { "Cat_PluginCLAP", "Wtyczka (CLAP)" },
        { "Cat_PluginAAX", "Wtyczka (AAX)" },
        { "Cat_App", "Aplikacja" },
        { "Cat_Config", "Plik konfiguracyjny / Log" },
        { "Cat_Presets", "Presety audio / Banki" },
        { "Cat_Library", "Biblioteka brzmień / Baza danych" },
        { "Cat_Other", "Inny plik wtyczki" },

        { "VST2 (systemowy)", "VST2 (systemowy)" },
        { "VST2 (użytkownik)", "VST2 (użytkownik)" },
        { "VST3 (systemowy)", "VST3 (systemowy)" },
        { "VST3 (użytkownik)", "VST3 (użytkownik)" },
        { "AudioUnit (systemowy)", "AudioUnit (systemowy)" },
        { "AudioUnit (użytkownik)", "AudioUnit (użytkownik)" },
        { "CLAP (systemowy)", "CLAP (systemowy)" },
        { "CLAP (użytkownik)", "CLAP (użytkownik)" },
        { "AAX / Pro Tools", "AAX / Pro Tools" },
        { "AAX / Pro Tools (użytkownik)", "AAX / Pro Tools (użytkownik)" },
        { "Biblioteka brzmień (Shared)", "Biblioteka brzmień (Shared)" },
        { "Ustawienia / dane", "Ustawienia / dane" },
        { "Native Instruments", "Native Instruments" },
        { "Steinberg", "Steinberg" },
        { "Preferencje", "Preferencje" },
        { "Preferencje NI", "Preferencje NI" },
        { "Plik licencji (ukryty)", "Plik licencji (ukryty)" },
        { "Logi", "Logi" },
        { "Cache", "Cache" },
        { "Receipt (pkgutil)", "Receipt (pkgutil)" },
        { "Presety audio", "Presety audio" },
        { "Dźwięki / samples", "Dźwięki / samples" },
        { "Dokumenty", "Dokumenty" },
        { "Aplikacja", "Aplikacja" },
        { "Plik konfiguracyjny / Log", "Plik konfiguracyjny / Log" },
        { "Presety audio / Banki", "Presety audio / Banki" },
        { "Inne", "Inne" },

        { "Err_SudoDenied", "Odmowa dostępu / błąd sudo:" },
        { "Err_SudoFail", "Nie udało się uruchomić osascript." },
        { "Err_InvalidChars", "Nazwa wtyczki zawiera niedozwolone znaki" },
        { "Err_TooShort", "Nazwa wtyczki musi mieć co najmniej 2 znaki" },
        { "Err_ManifestCorrupt", "[Ochrona] Plik manifestu jest uszkodzony lub pomyślnie edytowany (brak pola 'entries')." },
        { "Err_MissingDir", "[Pre-flight] Przerwano! Brak wymaganego folderu w kopii:" },
        { "Err_MissingFile", "[Pre-flight] Przerwano! Brak wymaganego pliku w kopii:" },
    };

    private readonly Dictionary<string, string> _en = new()
    {
        // ── MainWindow.axaml ────────────────────────────────────────────────
        { "AppTitle", "VST Deleter" },
        { "Phase1_Title", "Clean up VST & AU plugins" },
        { "Phase1_Subtitle", "Search for a plugin, review files and delete what you want." },
        { "Phase1_PluginNameLabel", "PLUGIN NAME" },
        { "Phase1_SearchPlaceholder", "e.g., Guitar Rig 6, Serum, Kontakt…" },
        { "Phase1_ScanBtn", "Scan" },
        { "Phase1_LocalPluginsLabel", "OR SELECT FROM INSTALLED" },
        { "Phase1_LocalPluginsPlaceholder", "Select a detected plugin..." },
        { "Phase1_LocalScanBtn", "Scan from system" },
        { "Phase1_DetectedFormats", "Detected formats:" },
        { "Phase1_Hint", "💡  Database contains 200+ known VST/AU/CLAP plugins. You can also type a custom name." },
        { "Phase1_RestoreBoxTitle", "Already have a backup?" },
        { "Phase1_RestoreBoxSub", "Load a manifest and restore previously deleted files." },
        { "Phase1_RestoreBtn", "Restore backup" },

        { "Phase2_Searching", "Searching for plugin traces…" },

        { "Phase3_ResultsFor", "Results for: {0}" },
        { "Phase3_FoundItemsCount", "Found {0} items · total {1}" },
        { "Phase3_NewSearchBtn", "New search" },
        { "Phase3_SelectAll", "Select all" },
        { "Phase3_DeselectAll", "Deselect all" },
        { "Phase3_SelectedLabel", "Selected: {0}" },
        { "Phase3_BackupCheckbox", "🔒  Create a backup before deleting" },
        { "Phase3_BackupSelectFolder", "(click Select…)" },
        { "Phase3_BackupChooseBtn", "Select…" },
        { "Phase3_NoResultsTitle", "No traces found" },
        { "Phase3_NoResultsSub", "System is clean. This plugin left no traces." },
        { "Phase3_DeleteSummary", "To delete: {0}  ·  Size: {1}" },
        { "Phase3_CancelBtn", "Cancel" },
        { "Phase3_BackupOnlyBtn", "Backup only" },
        { "Phase3_BackupOnlyTooltip", "Requires a selected backup folder" },
        { "Phase3_DeleteSelectedBtn", "Delete selected" },

        { "Phase4_Deleting", "Deleting files…" },

        { "Phase5_BackupSaved", "🔒  Backup saved:" },
        { "Phase5_PreviewBtn", "Preview →" },
        { "Phase5_SystemFilesHint", "💡 System files require administrator privileges (run with sudo)." },
        { "Phase5_BackToResultsBtn", "← Back to results" },

        { "Phase5b_BackingUp", "Creating backup…" },
        { "Phase5b_WaitHint", "Please don't close the app — creating backup…" },

        { "Phase6_RestoreTitle", "Restore backup" },
        { "Phase6_RestoreSub", "Select the manifest file (VSTDeleter_manifest.json) from your backup folder.\nThe manifest contains a map of all files and their original locations." },
        { "Phase6_RestoreClickBelow", "Click below to select the backup manifest" },
        { "Phase6_ChooseManifestBtn", "Select VSTDeleter_manifest.json" },
        { "Phase6_ManifestLoaded", "✓  Manifest loaded successfully:" },
        { "Phase6_BackBtn", "← Back" },
        { "Phase6_PreviewRestoreBtn", "Preview and restore" },

        { "Phase7_ReviewTitle", "Restore preview" },
        { "Phase7_BackupFolderLabel", "Backup folder:" },
        { "Phase7_RestoreSelectedBtn", "Restore {0} files" },

        { "Phase8_Restoring", "Restoring files..." },
        { "Phase9_SuccessTitle", "Done!" },

        // ── MainViewModel.cs ────────────────────────────────────────────────
        { "Log_FatalError", "Fatal error" },
        { "Scan_Searching", "Searching traces:" },
        { "Scan_Checking", "Checking:" },
        { "Scan_QueryError", "⚠ Query error:" },
        { "Backup_SelectFolderDialog", "Select backup folder" },
        { "Backup_EstimatedSize", "Estimated backup: ~{0}  →  {1}" },
        { "Backup_NoFolder", "(select folder)" },
        { "Backup_Preparing", "Preparing backup…" },
        { "Backup_Error", "Error:" },
        { "Backup_OnlySuccess", "✓ Backup saved. No files were deleted." },
        { "Delete_FolderRequired", "To continue, select a destination folder for the backup (or uncheck the option)." },
        { "Delete_AbortedSafe", "Operation aborted for safety reasons." },
        { "Delete_BackupError", "Backup error:" },
        { "Delete_DeletingItems", "Deleting selected items…" },
        { "Delete_Success", "✓ Successfully deleted {0} item(s)." },
        { "Delete_Partial", "Deleted {0}, errors: {1}." },
        { "Manifest_SelectDialog", "Select backup manifest file (VSTDeleter_manifest.json)" },
        { "Manifest_Invalid", "⚠ Cannot load file — make sure it is a valid VST Deleter manifest." },
        { "Restore_HeaderInfo", "Plugin: {0}  ·  Backup from: {1:dd.MM.yyyy HH:mm}  ·  {2} items" },
        { "Restore_SuccessSkipped", "✓ Restored {0} files. (Skipped {1} SIP receipts, plugins will still work)" },
        { "Restore_Success", "✓ Successfully restored {0} item(s)." },
        { "Restore_Partial", "Restored {0}, errors: {1}, skipped: {2}." },

        // ── Usługi pod spodem ────────────────────────────────────────────────
        { "Cat_PluginAU", "Plugin (AudioUnit)" },
        { "Cat_PluginVST", "Plugin (VST)" },
        { "Cat_PluginVST3", "Plugin (VST3)" },
        { "Cat_PluginCLAP", "Plugin (CLAP)" },
        { "Cat_PluginAAX", "Plugin (AAX)" },
        { "Cat_App", "Application" },
        { "Cat_Config", "Config file / Log" },
        { "Cat_Presets", "Audio presets / Banks" },
        { "Cat_Library", "Sound library / Database" },
        { "Cat_Other", "Other plugin file" },

        { "VST2 (systemowy)", "VST2 (system)" },
        { "VST2 (użytkownik)", "VST2 (user)" },
        { "VST3 (systemowy)", "VST3 (system)" },
        { "VST3 (użytkownik)", "VST3 (user)" },
        { "AudioUnit (systemowy)", "AudioUnit (system)" },
        { "AudioUnit (użytkownik)", "AudioUnit (user)" },
        { "CLAP (systemowy)", "CLAP (system)" },
        { "CLAP (użytkownik)", "CLAP (user)" },
        { "AAX / Pro Tools", "AAX / Pro Tools" },
        { "AAX / Pro Tools (użytkownik)", "AAX / Pro Tools (user)" },
        { "Biblioteka brzmień (Shared)", "Sound library (Shared)" },
        { "Ustawienia / dane", "Settings / Data" },
        { "Native Instruments", "Native Instruments" },
        { "Steinberg", "Steinberg" },
        { "Preferencje", "Preferences" },
        { "Preferencje NI", "NI Preferences" },
        { "Plik licencji (ukryty)", "License file (hidden)" },
        { "Logi", "Logs" },
        { "Cache", "Cache" },
        { "Receipt (pkgutil)", "Receipt (pkgutil)" },
        { "Presety audio", "Audio presets" },
        { "Dźwięki / samples", "Sounds / samples" },
        { "Dokumenty", "Documents" },
        { "Aplikacja", "Application" },
        { "Plik konfiguracyjny / Log", "Config file / Log" },
        { "Presety audio / Banki", "Audio presets / Banks" },
        { "Inne", "Other" },

        { "Err_SudoDenied", "Access denied / sudo error:" },
        { "Err_SudoFail", "Failed to launch osascript." },
        { "Err_InvalidChars", "Plugin name contains invalid characters" },
        { "Err_TooShort", "Plugin name must be at least 2 characters long" },
        { "Err_ManifestCorrupt", "[Protection] Manifest file is corrupted or manually edited (missing 'entries')." },
        { "Err_MissingDir", "[Pre-flight] Aborted! Missing required folder in backup:" },
        { "Err_MissingFile", "[Pre-flight] Aborted! Missing required file in backup:" },
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
