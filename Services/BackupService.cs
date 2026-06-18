using System.Text.Json;
using System.Text.Encodings.Web;
using VstDeleter.Models;

namespace VstDeleter.Services;

/// <summary>
/// Tworzy i przywraca kopie zapasowe usuniętych plików/katalogów.
/// </summary>
public static class BackupService
{
    // JSON z wcięciami i bez escapowania polskich znaków
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public const string ManifestFileName = "VSTDeleter_manifest.json";

    // ─────────────────────────────────────────────────────────────────────────
    //  Tworzenie kopii zapasowej
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Kopiuje wszystkie elementy z <paramref name="items"/> do folderu kopii,
    /// zachowując pełną strukturę ścieżki, i zapisuje manifest JSON.
    /// Zwraca ścieżkę do pliku manifestu.
    /// </summary>
    public static async Task<string> CreateBackupAsync(
        string pluginName,
        IEnumerable<FoundItem> items,
        string backupRootFolder,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        // Nazwa folderu kopii: VSTDeleter_Backup_GuitarRig6_20260617_120000
        string timestamp  = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        string safeName   = MakeSafe(pluginName);
        string backupDir  = Path.Combine(backupRootFolder, $"VSTDeleter_Backup_{safeName}_{timestamp}");
        Directory.CreateDirectory(backupDir);

        var entries = new List<BackupEntry>();
        int errorsCount = 0;

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            // Ścieżka relatywna: usuwa wiodący '/' → staje się podkatalogiem w backupDir
            string relativePath = item.Path.TrimStart('/');
            string targetPath   = Path.Combine(backupDir, relativePath);

            try
            {
                progress?.Report(item.ShortPath);

                await DittoCopyAsync(item.Path, targetPath, ct);

                entries.Add(new BackupEntry
                {
                    OriginalAbsolutePath = item.Path,
                    BackupRelativePath   = relativePath,
                    Type                 = item.IsDirectory ? "directory" : "file",
                    Category             = item.Category,
                    SizeBytes            = item.SizeBytes
                });
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                errorsCount++;
                AppLogger.Log($"[backup] {item.Path}: {ex.ToString()}");
            }
        }

        if (errorsCount > 0)
        {
            throw new Exception($"Zakończono z błędami. {errorsCount} element(ów) nie zostało skopiowanych. Zatrzymano operację, aby zapobiec utracie danych.");
        }

        // Zapis manifestu
        var manifest = new BackupManifest
        {
            PluginName  = pluginName,
            Created     = DateTimeOffset.Now,
            BackupFolder = backupDir,
            Entries     = entries
        };

        string manifestPath = Path.Combine(backupDir, ManifestFileName);
        string json = JsonSerializer.Serialize(manifest, JsonOptions);
        await File.WriteAllTextAsync(manifestPath, json, System.Text.Encoding.UTF8, ct);

        return manifestPath;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Wczytanie manifestu
    // ─────────────────────────────────────────────────────────────────────────

    public static BackupManifest? LoadManifest(string manifestPath)
    {
        try
        {
            string json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<BackupManifest>(json, JsonOptions);
            if (manifest != null)
            {
                // Dynamiczna ścieżka - umożliwia przenoszenie kopii na dyskach zewnętrznych
                manifest.BackupFolder = Path.GetDirectoryName(manifestPath) ?? string.Empty;
            }
            return manifest;
        }
        catch (Exception ex)
        {
            AppLogger.Log($"[restore] Nie można wczytać manifestu: {ex.ToString()}");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Przywracanie z kopii
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Przywraca wszystkie wpisy z manifestu z powrotem na oryginalne ścieżki.
    /// Nadpisuje istniejące pliki.
    /// </summary>
    public static async Task<RestoreResult> RestoreBackupAsync(
        BackupManifest manifest,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(async () =>
        {
            int ok = 0, errors = 0, skipped = 0;
            var errorMessages = new List<string>();
            var sudoCopyTasks = new List<(string source, string dest)>();

            if (manifest.Entries == null)
            {
                errorMessages.Add("[Ochrona] Plik manifestu jest uszkodzony lub pomyślnie edytowany (brak pola 'entries').");
                return new RestoreResult(0, 1, 0, errorMessages);
            }

            // Pre-flight check (Zapobieganie Zombie Restore)
            foreach (var entry in manifest.Entries)
            {
                string checkPath = Path.Combine(manifest.BackupFolder, entry.BackupRelativePath);
                if (entry.Type == "directory" && !Directory.Exists(checkPath))
                {
                    errorMessages.Add($"[Pre-flight] Przerwano! Brak wymaganego folderu w kopii: {checkPath}");
                    return new RestoreResult(0, 1, 0, errorMessages);
                }
                else if (entry.Type != "directory" && !File.Exists(checkPath))
                {
                    errorMessages.Add($"[Pre-flight] Przerwano! Brak wymaganego pliku w kopii: {checkPath}");
                    return new RestoreResult(0, 1, 0, errorMessages);
                }
            }

            foreach (var entry in manifest.Entries)
            {
                ct.ThrowIfCancellationRequested();

                string sourcePath = Path.Combine(manifest.BackupFolder, entry.BackupRelativePath);
                string destPath   = entry.OriginalAbsolutePath;

                if (destPath.Contains("/var/db/receipts/", StringComparison.OrdinalIgnoreCase) ||
                    destPath.Contains("/private/var/db/receipts/", StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                    AppLogger.Log($"[Przywracanie] Zignorowano paragon instalacyjny (ochrona SIP blokuje zapis): {destPath}");
                    continue;
                }

                try
                {
                    progress?.Report(entry.ShortOriginalPath);

                    await DittoCopyAsync(sourcePath, destPath, ct);

                    ok++;
                }
                catch (UnauthorizedAccessException)
                {
                    sudoCopyTasks.Add((sourcePath, destPath));
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    errors++;
                    errorMessages.Add($"{destPath}: {ex.Message}"); // Wiadomość dla UI
                    AppLogger.Log($"[restore] {destPath}: {ex.ToString()}"); // Pełny stack dla logów
                }
            }

            if (sudoCopyTasks.Count > 0)
            {
                try
                {
                    progress?.Report("Wymagane uprawnienia administratora do przywrócenia...");
                    
                    var bashLines = new List<string>();
                    string currentUser = Environment.UserName;
                    
                    foreach (var task in sudoCopyTasks)
                    {
                        string safeSource = $"'{task.source.Replace("'", "'\\''")}'";
                        
                        string destDir = Path.GetDirectoryName(task.dest) ?? "";
                        string safeDestDir = $"'{destDir.Replace("'", "'\\''")}'";
                        
                        string safeDest = $"'{task.dest.Replace("'", "'\\''")}'";

                        bashLines.Add($"mkdir -p {safeDestDir}");
                        bashLines.Add($"ditto {safeSource} {safeDest}");
                        
                        if (task.dest.StartsWith("/Library/") || task.dest.StartsWith("/Applications/"))
                        {
                            bashLines.Add($"chown -R root:wheel {safeDest}");
                        }
                        else
                        {
                            bashLines.Add($"chown -R {currentUser}:staff {safeDest}");
                        }
                    }
                    
                    string bashCommand = string.Join(" ; ", bashLines);
                    string scriptPath = Path.GetTempFileName();
                    
                    try
                    {
                        await File.WriteAllTextAsync(scriptPath, bashCommand, ct);

                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "osascript",
                            UseShellExecute = false,
                            RedirectStandardOutput = false,
                            RedirectStandardError = true
                        };
                        
                        psi.ArgumentList.Add("-e");
                        psi.ArgumentList.Add("on run argv");
                        psi.ArgumentList.Add("-e");
                        psi.ArgumentList.Add("do shell script \"/bin/sh '\" & item 1 of argv & \"'\" with administrator privileges");
                        psi.ArgumentList.Add("-e");
                        psi.ArgumentList.Add("end run");
                        psi.ArgumentList.Add(scriptPath);
                        
                        using var proc = System.Diagnostics.Process.Start(psi);
                        if (proc != null)
                        {
                            string err = await proc.StandardError.ReadToEndAsync();
                            await proc.WaitForExitAsync();
                            
                            if (proc.ExitCode == 0)
                            {
                                ok += sudoCopyTasks.Count;
                            }
                            else
                            {
                                errors += sudoCopyTasks.Count;
                                errorMessages.Add($"Odmowa dostępu / błąd sudo przy przywracaniu: {err.Trim()}");
                            }
                        }
                        else
                        {
                            errors += sudoCopyTasks.Count;
                            errorMessages.Add("Nie udało się uruchomić osascript (sudo).");
                        }
                    }
                    finally
                    {
                        if (File.Exists(scriptPath)) File.Delete(scriptPath);
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    errors += sudoCopyTasks.Count;
                    errorMessages.Add($"Brak programu osascript w systemie (przywracanie): {ex.Message}");
                    AppLogger.Log($"[restore sudo] osascript missing: {ex.ToString()}");
                }
                catch (InvalidOperationException ex)
                {
                    errors += sudoCopyTasks.Count;
                    errorMessages.Add($"Błąd procesu sudo (przywracanie): {ex.Message}");
                    AppLogger.Log($"[restore sudo] process error: {ex.ToString()}");
                }
                catch (Exception ex)
                {
                    errors += sudoCopyTasks.Count;
                    errorMessages.Add($"Nieoczekiwany błąd Sudo (przywracanie): {ex.Message}");
                    AppLogger.Log($"[restore sudo] unexpected: {ex.ToString()}");
                }
            }

            return new RestoreResult(ok, errors, skipped, errorMessages);
        }, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Prywatne helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task DittoCopyAsync(string source, string dest, CancellationToken ct)
    {
        string destDir = Path.GetDirectoryName(dest) ?? "";
        if (!string.IsNullOrEmpty(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ditto",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add(source);
        psi.ArgumentList.Add(dest);

        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc != null)
        {
            using var ctr = ct.Register(() => { try { proc.Kill(true); } catch { } });
            string err = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            if (proc.ExitCode != 0)
            {
                if (err.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) || 
                    err.Contains("Operation not permitted", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException(err.Trim());
                }
                throw new Exception($"Polecenie ditto zwróciło błąd (kod {proc.ExitCode}) dla ścieżki: {source}. Detale: {err.Trim()}");
            }
        }
    }

    /// <summary>Usuwa znaki niebezpieczne z nazwy folderu.</summary>
    private static string MakeSafe(string name) =>
        new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}

public record RestoreResult(int Restored, int Errors, int Skipped, List<string> ErrorMessages);
