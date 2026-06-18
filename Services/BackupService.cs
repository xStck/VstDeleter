using System.Text.Json;
using System.Text.Encodings.Web;
using VstDeleter.Helpers;
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
        var sudoBackupTasks = new List<FoundItem>();

        try
        {
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
                catch (Exception)
                {
                    // Plik zablokowany np. systemowym zabezpieczeniem w /Library. Przekazujemy do eskalacji.
                    sudoBackupTasks.Add(item);
                }
            }

            // Próba ratunkowa z prawami Sudo dla plików opornych
            if (sudoBackupTasks.Count > 0)
            {
                string tempScriptFile = Path.Combine(Path.GetTempPath(), $"vstdeleter_backup_{Guid.NewGuid():N}.sh");
                string tempFlagFile = Path.Combine(Path.GetTempPath(), $"vstdeleter_backup_{Guid.NewGuid():N}.flag");
                try
                {
                    await File.WriteAllTextAsync(tempFlagFile, "1", ct);
                    var scriptLines = new List<string> { "#!/bin/bash", "SUCC=0", "ERR=0" };
                    foreach (var item in sudoBackupTasks)
                    {
                        string relativePath = item.Path.TrimStart('/');
                        string targetPath   = Path.Combine(backupDir, relativePath);
                        string safeSource   = $"'{BashHelpers.Escape(item.Path)}'";
                        string safeDest     = $"'{BashHelpers.Escape(targetPath)}'";
                        string safeDestDir  = $"'{BashHelpers.Escape(Path.GetDirectoryName(targetPath) ?? "")}'";

                        string cmd = $"(mkdir -p {safeDestDir} && ditto {safeSource} {safeDest})";
                        scriptLines.Add($"[ ! -f '{BashHelpers.Escape(tempFlagFile)}' ] && exit 1");
                        scriptLines.Add($"{cmd} && SUCC=$((SUCC+1)) || ERR=$((ERR+1))");
                    }
                    scriptLines.Add("echo \"SUCCESS:$SUCC\"");
                    scriptLines.Add("echo \"ERRORS:$ERR\"");

                    await File.WriteAllLinesAsync(tempScriptFile, scriptLines, ct);

                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "osascript",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    
                    using var proc = System.Diagnostics.Process.Start(psi);
                    if (proc != null)
                    {
                        using var ctr = ct.Register(() => { 
                            try { if (File.Exists(tempFlagFile)) File.Delete(tempFlagFile); } catch { }
                            try { proc.Kill(true); } catch { } 
                        });
                        string safeScriptPath = $"'{BashHelpers.Escape(tempScriptFile)}'";
                        await proc.StandardInput.WriteAsync($"with timeout of 86400 seconds\n do shell script \"sh {safeScriptPath}\" with administrator privileges\n end timeout");
                        proc.StandardInput.Close();

                        var outTask = proc.StandardOutput.ReadToEndAsync(ct);
                        var errTask = proc.StandardError.ReadToEndAsync(ct);
                        await Task.WhenAll(outTask, errTask);
                        string outStr = await outTask;
                        string errStr = await errTask;
                        await proc.WaitForExitAsync(ct);
                        
                        if (proc.ExitCode == 0 && outStr.Contains("SUCCESS:"))
                        {
                            // Niezależnie od mniejszych zgrzytów wewnątrz skryptu powłoki dopisujemy wydelegowane ratunkowe pliki do manifestu
                            foreach (var item in sudoBackupTasks)
                            {
                                string relativePath = item.Path.TrimStart('/');
                                string checkPath = Path.Combine(backupDir, relativePath);
                                if (item.IsDirectory ? Directory.Exists(checkPath) : File.Exists(checkPath))
                                {
                                    entries.Add(new BackupEntry
                                    {
                                        OriginalAbsolutePath = item.Path,
                                        BackupRelativePath   = relativePath,
                                        Type                 = item.IsDirectory ? "directory" : "file",
                                        Category             = item.Category,
                                        SizeBytes            = item.SizeBytes
                                    });
                                }
                            }

                            int parsedErr = 0;
                            foreach (var outputLine in outStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (outputLine.StartsWith("ERRORS:")) int.TryParse(outputLine.Substring(7), out parsedErr);
                            }
                            if (parsedErr > 0)
                            {
                                errorsCount += parsedErr;
                                AppLogger.Log($"[backup] Ostrzeżenie Sudo: {parsedErr} wysoce chronionych plików nie dało się skopiować nawet jako Root.");
                            }
                        }
                        else
                        {
                            errorsCount += sudoBackupTasks.Count;
                            AppLogger.Log($"[backup] Eskalacja Root odrzucona dla {sudoBackupTasks.Count} plików. Treść: {errStr.Trim()} {outStr.Trim()}");
                        }
                    }
                }
                finally
                {
                    try { if (File.Exists(tempScriptFile)) File.Delete(tempScriptFile); } catch { }
                    try { if (File.Exists(tempFlagFile)) File.Delete(tempFlagFile); } catch { }
                }
            }

            if (errorsCount > 0)
            {
                AppLogger.Log($"[backup] Uwaga: Manifest utworzony z brakami. Nieskopiowano {errorsCount} element(ów). Reszta dysku bezpieczna.");
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
        catch
        {
            // Ochrona przed dyskowymi "sierotami". Jeżeli cokolwiek pękło przed pomyślnym zapisaniem json'a
            // usuwamy nowostworzony katalog z dysku, zwalniając miejsce (niedokończony backup i tak nie zadziała).
            if (Directory.Exists(backupDir))
            {
                try
                {
                    Directory.Delete(backupDir, true);
                }
                catch (Exception cleanupEx)
                {
                    AppLogger.Log($"[backup] Krytyczne: nie udało się usunąć uszkodzonego folderu kopii: {cleanupEx.Message}");
                }
            }
            
            throw; // Przerzucamy wyżej, by UI wiedziało o awarii
        }
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

                if (!PluginScanner.IsPathSafeToDelete(destPath))
                {
                    skipped++;
                    AppLogger.Log($"[Ochrona] Zablokowano próbę modyfikacji krytycznej ścieżki (Podatność JSON): {destPath}");
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
                    
                    var taskCommands = new List<string>();
                    string currentUser = Environment.UserName;
                    
                    foreach (var task in sudoCopyTasks)
                    {
                        var singleTaskLines = new List<string>();
                        string safeSource = $"'{BashHelpers.Escape(task.source)}'";
                        
                        string destDir = Path.GetDirectoryName(task.dest) ?? "";
                        string safeDestDir = $"'{BashHelpers.Escape(destDir)}'";
                        
                        string safeDest = $"'{BashHelpers.Escape(task.dest)}'";

                        singleTaskLines.Add($"mkdir -p {safeDestDir}");
                        singleTaskLines.Add($"ditto {safeSource} {safeDest}");
                        
                        if (task.dest.StartsWith("/Library/") || task.dest.StartsWith("/Applications/") || task.dest.StartsWith("/Users/Shared/"))
                        {
                            singleTaskLines.Add($"chown -R root:wheel {safeDest}");
                            // Opcjonalnie: dajemy pełne uprawnienia dla demonów audio
                            singleTaskLines.Add($"chmod -R 777 {safeDest}");
                        }
                        else
                        {
                            singleTaskLines.Add($"chown -R '{BashHelpers.Escape(currentUser)}':staff {safeDest}");
                        }
                        
                        taskCommands.Add("(" + string.Join(" && ", singleTaskLines) + ")");
                    }
                    
                    int actualSudoCommands = taskCommands.Count;
                    string tempScriptFile = Path.Combine(Path.GetTempPath(), $"vstdeleter_rest_{Guid.NewGuid():N}.sh");
                    string tempFlagFile = Path.Combine(Path.GetTempPath(), $"vstdeleter_rest_{Guid.NewGuid():N}.flag");
                    
                    try
                    {
                        await File.WriteAllTextAsync(tempFlagFile, "1", ct);
                        var scriptLines = new List<string> { "#!/bin/bash", "SUCC=0", "ERR=0" };
                        foreach (var cmd in taskCommands)
                        {
                            scriptLines.Add($"[ ! -f '{BashHelpers.Escape(tempFlagFile)}' ] && exit 1");
                            scriptLines.Add($"{cmd} && SUCC=$((SUCC+1)) || ERR=$((ERR+1))");
                        }
                        scriptLines.Add("echo \"SUCCESS:$SUCC\"");
                        scriptLines.Add("echo \"ERRORS:$ERR\"");

                        await File.WriteAllLinesAsync(tempScriptFile, scriptLines, ct);

                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "osascript",
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        
                        using var proc = System.Diagnostics.Process.Start(psi);
                        if (proc != null)
                        {
                            using var ctr = ct.Register(() => { 
                                try { if (File.Exists(tempFlagFile)) File.Delete(tempFlagFile); } catch { }
                                try { proc.Kill(true); } catch { } 
                            });
                            
                            string safeScriptPath = $"'{BashHelpers.Escape(tempScriptFile)}'";
                            await proc.StandardInput.WriteAsync($"with timeout of 86400 seconds\n do shell script \"sh {safeScriptPath}\" with administrator privileges\n end timeout");
                            proc.StandardInput.Close();

                            var outTask = proc.StandardOutput.ReadToEndAsync(ct);
                            var errTask = proc.StandardError.ReadToEndAsync(ct);
                            await Task.WhenAll(outTask, errTask);
                            string outStr = await outTask;
                            string errStr = await errTask;
                            await proc.WaitForExitAsync(ct);
                            
                            if (proc.ExitCode == 0 && outStr.Contains("SUCCESS:"))
                            {
                                int parsedOk = 0;
                                int parsedErr = 0;
                                foreach (var outputLine in outStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (outputLine.StartsWith("SUCCESS:")) int.TryParse(outputLine.Substring(8), out parsedOk);
                                    if (outputLine.StartsWith("ERRORS:")) int.TryParse(outputLine.Substring(7), out parsedErr);
                                }
                                ok += parsedOk;
                                errors += parsedErr;
                                if (parsedErr > 0)
                                {
                                    errorMessages.Add($"Odtwarzanie Sudo częściowo zablokowane. Pomyślnie: {parsedOk}, Błędy: {parsedErr}");
                                }
                            }
                            else
                            {
                                errors += actualSudoCommands;
                                errorMessages.Add($"Odmowa dostępu / błąd sudo przy przywracaniu: {errStr.Trim()} {outStr.Trim()}");
                            }
                        }
                        else
                        {
                            errors += actualSudoCommands;
                            errorMessages.Add("Nie udało się uruchomić osascript (sudo).");
                        }
                    }
                    finally
                    {
                        try { if (File.Exists(tempScriptFile)) File.Delete(tempScriptFile); } catch { }
                        try { if (File.Exists(tempFlagFile)) File.Delete(tempFlagFile); } catch { }
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    errors += sudoCopyTasks.Count;  // fallback: nie wiemy ile komend wygenerowano
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

        if (File.Exists(source))
        {
            try
            {
                await Task.Run(() => File.Copy(source, dest, true), ct);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let it bubble up so sudo logic can take over
            }
            catch (Exception)
            {
                // If native copy fails for other reasons, fallback to ditto just in case
            }
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
