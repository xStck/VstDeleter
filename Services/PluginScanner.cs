using VstDeleter.Models;
using VstDeleter.Helpers;

namespace VstDeleter.Services;

/// <summary>
/// Skanuje system macOS w poszukiwaniu śladów po wtyczce.
/// </summary>
public static class PluginScanner
{
    public static async Task<List<FoundItem>> ScanAsync(
        string pluginName,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pluginName) || pluginName.Trim().Length < 2)
            throw new ArgumentException(LanguageService.Instance["Err_TooShort"]);

        string pNameLow = pluginName.ToLower().Trim();
        string[] blockedTerms = { "apple", "mac", "macos", "system", "library", "usr", "bin", "var", "etc", "com", "org", "net", "app", "application" };
        if (blockedTerms.Contains(pNameLow))
            throw new ArgumentException("Wyszukiwana fraza jest zbyt ogólna lub zablokowana ze względów bezpieczeństwa.");

        string[] invalidChars = { "/", "\\", ".", "*", "?", "<", ">", "|", ":" };
        foreach (var c in invalidChars)
        {
            if (pluginName.Contains(c))
                throw new ArgumentException($"{LanguageService.Instance["Err_InvalidChars"]}: {c}");
        }

        return await Task.Run(() => Scan(pluginName, progress, ct), ct);
    }

    private static List<FoundItem> Scan(
        string pluginName,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var variants = BuildNameVariants(pluginName).ToList();
        var candidates = BuildCandidatePaths(pluginName, variants, home, ct);

        var found = new List<FoundItem>();

        foreach (var (path, category) in candidates)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                bool isDir  = Directory.Exists(path);
                bool isFile = !isDir && File.Exists(path);

                if (!isDir && !isFile) continue;

                progress?.Report(path);

                long size = isDir ? GetDirectorySize(path, ct) : new FileInfo(path).Length;
                found.Add(new FoundItem
                {
                    Path        = path,
                    IsDirectory = isDir,
                    SizeBytes   = size,
                    Category    = category,
                    IsSelected  = category.StartsWith("Wtyczka") || category == "Aplikacja"
                });
            }
            catch (UnauthorizedAccessException ex) { AppLogger.Log($"[ScanAsync Brak dostępu]: {ex.ToString()}"); }
            catch (IOException ex)                 { AppLogger.Log($"[ScanAsync Błąd I/O]: {ex.ToString()}"); }
            catch (Exception ex)                   { AppLogger.Log($"[ScanAsync Błąd ogólny]: {ex.ToString()}"); }
        }

        return found;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Budowanie listy ścieżek z kategoriami
    // ─────────────────────────────────────────────────────────────────────────
    private static IEnumerable<(string path, string category)> BuildCandidatePaths(
        string pluginName, List<string> variants, string home, CancellationToken ct)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(List<(string, string)> list, string path, string cat)
        {
            string p = path.TrimEnd('/');
            if (p.Equals($"{home}/.Trash", StringComparison.OrdinalIgnoreCase) ||
                p.Equals($"{home}/.ssh", StringComparison.OrdinalIgnoreCase) ||
                p.Equals($"{home}/.config", StringComparison.OrdinalIgnoreCase) ||
                p.Equals($"{home}/.local", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("/Applications/Utilities", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("/Library", StringComparison.OrdinalIgnoreCase) ||
                p.Equals($"{home}/Library", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (seen.Add(path)) list.Add((path, cat));
        }

        var result = new List<(string, string)>();

        foreach (string v in variants)
        {
            // VST2
            Add(result, $"/Library/Audio/Plug-Ins/VST/{v}.vst",               "VST2 (systemowy)");
            Add(result, $"{home}/Library/Audio/Plug-Ins/VST/{v}.vst",         "VST2 (użytkownik)");

            // VST3
            Add(result, $"/Library/Audio/Plug-Ins/VST3/{v}.vst3",             "VST3 (systemowy)");
            Add(result, $"{home}/Library/Audio/Plug-Ins/VST3/{v}.vst3",       "VST3 (użytkownik)");

            // AU / AudioUnit
            Add(result, $"/Library/Audio/Plug-Ins/Components/{v}.component",  "AudioUnit (systemowy)");
            Add(result, $"{home}/Library/Audio/Plug-Ins/Components/{v}.component", "AudioUnit (użytkownik)");

            // CLAP
            Add(result, $"/Library/Audio/Plug-Ins/CLAP/{v}.clap",             "CLAP (systemowy)");
            Add(result, $"{home}/Library/Audio/Plug-Ins/CLAP/{v}.clap",       "CLAP (użytkownik)");

            // AAX (Pro Tools)
            Add(result, $"/Library/Application Support/Avid/Audio/Plug-Ins/{v}.aaxplugin",        "AAX / Pro Tools");
            Add(result, $"{home}/Library/Application Support/Avid/Audio/Plug-Ins/{v}.aaxplugin",  "AAX / Pro Tools (użytkownik)");

            // Shared — biblioteki brzmień (NI, Arturia, Spectrasonics itp.)
            Add(result, $"/Users/Shared/{v}",  "Biblioteka brzmień (Shared)");

            // Application Support
            Add(result, $"{home}/Library/Application Support/{v}",            "Ustawienia / dane");
            Add(result, $"/Library/Application Support/{v}",                  "Ustawienia / dane");
            Add(result, $"{home}/Library/Application Support/Native Instruments/{v}", "Native Instruments");
            Add(result, $"{home}/Library/Application Support/Steinberg/{v}",  "Steinberg");

            // Preferences / plist
            Add(result, $"{home}/Library/Preferences/{v}.plist",              "Preferencje");
            Add(result, $"{home}/Library/Preferences/com.{Slug(v)}.plist",    "Preferencje");
            Add(result, $"{home}/Library/Preferences/com.native-instruments.{Slug(v)}.plist", "Preferencje NI");

            // Ukryte pliki licencji w katalogu domowym
            Add(result, $"{home}/.{v}",                  "Plik licencji (ukryty)");
            Add(result, $"{home}/.{v.Replace(" ", "")}",  "Plik licencji (ukryty)");
            Add(result, $"{home}/.{v.Replace(" ", "_")}",  "Plik licencji (ukryty)");
            Add(result, $"{home}/.{v.ToLower()}",          "Plik licencji (ukryty)");
            Add(result, $"{home}/.{Slug(v)}",              "Plik licencji (ukryty)");

            // Cache i logi
            Add(result, $"{home}/Library/Logs/{v}",                           "Logi");
            Add(result, $"{home}/Library/Caches/{v}",                         "Cache");
            Add(result, $"{home}/Library/Caches/com.{Slug(v)}",               "Cache");

            // Receipts pkgutil
            Add(result, $"/var/db/receipts/com.{Slug(v)}.pkg.bom",            "Receipt (pkgutil)");
            Add(result, $"/var/db/receipts/com.{Slug(v)}.pkg.plist",          "Receipt (pkgutil)");

            // Presets audio
            Add(result, $"/Library/Audio/Presets/{v}",                        "Presety audio");
            Add(result, $"{home}/Library/Audio/Presets/{v}",                  "Presety audio");
            Add(result, $"/Library/Audio/Sounds/{v}",                         "Dźwięki / samples");
            Add(result, $"{home}/Library/Audio/Sounds/{v}",                   "Dźwięki / samples");

            // Dokumenty
            Add(result, $"{home}/Documents/{v}",                              "Dokumenty");

            // Aplikacje
            Add(result, $"/Applications/{v}.app",                             "Aplikacja");
            Add(result, $"{home}/Applications/{v}.app",                       "Aplikacja");
        }

        // --- 4. Skanowanie "dynamiczne" głębokie (MaxDepth = 2) ---
        string norm = pluginName.Replace(" ", "");
        if (norm.Length >= 4)
        {
            string[] scanDirs = {
                "/var/db/receipts",
                $"{home}/Library/Preferences",
                "/Library/Audio/Presets",
                $"{home}/Library/Audio/Presets",
                "/Library/Application Support",
                $"{home}/Library/Application Support",
                "/Users/Shared"
            };

            var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, MaxRecursionDepth = 2 };

            foreach (var dir in scanDirs)
            {
                if (!Directory.Exists(dir)) continue;

                try
                {
                    foreach (var path in Directory.EnumerateFileSystemEntries(dir, "*", options))
                    {
                        ct.ThrowIfCancellationRequested();
                        
                        string fileName = Path.GetFileName(path).ToLower();

                        // Systemowa Czarna Lista (Blacklist)
                        if (fileName.StartsWith("com.apple.") || 
                            fileName.StartsWith("org.cups.") || 
                            fileName.StartsWith("com.microsoft.") ||
                            fileName.Equals("apple") ||
                            fileName.Equals("mac") ||
                            fileName.Equals("system") ||
                            fileName.Equals("library"))
                        {
                            continue;
                        }

                        bool isMatch = false;
                        bool isConfigPath = dir.Contains("Preferences", StringComparison.OrdinalIgnoreCase) || dir.Contains("receipts", StringComparison.OrdinalIgnoreCase);

                        foreach (var variant in variants)
                        {
                            string lowerVariant = variant.ToLowerInvariant();

                            if (isConfigPath)
                            {
                                // Pliki preferencji w macOS (np. com.nativeinstruments.massive.plist) często mają odwróconą domenę na początku.
                                // Użycie StartsWith("massive") ucinałoby te ważne pliki konfiguracyjne.
                                // Dla katalogów konfiguracji dopuszczamy luźniejsze dopasowanie (rzadko leżą tu gigabajty cudzych danych).
                                if (fileName.Contains(lowerVariant))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                            else
                            {
                                // Ochrona "Shotgun" dla folderów z ciężkimi assetami (Application Support, Presets).
                                // Zostawiamy restrykcyjne StartsWith. 
                                // ALE jeśli wpisana fraza jest unikalna i dłuższa niż 6 znaków (np. "Omnisphere"), 
                                // ryzyko fałszywych dopasowań drastycznie maleje i możemy bezpiecznie użyć Contains.
                                if (fileName.StartsWith(lowerVariant) || fileName.StartsWith($"com.{lowerVariant}") || fileName.StartsWith($"org.{lowerVariant}"))
                                {
                                    isMatch = true;
                                    break;
                                }
                                else if (lowerVariant.Length >= 6 && fileName.Contains(lowerVariant))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                        }

                        if (isMatch)
                        {
                            string category = "Plik konfiguracyjny / Log";
                            if (dir.Contains("Presets", StringComparison.OrdinalIgnoreCase))
                                category = "Presety audio / Banki";
                            else if (dir.Contains("Application Support", StringComparison.OrdinalIgnoreCase) || dir.Contains("Shared", StringComparison.OrdinalIgnoreCase))
                                category = "Biblioteka brzmień / Baza danych";
                                
                            Add(result, path, category);
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { AppLogger.Log($"[BuildPaths Dynamic Scan]: {ex.ToString()}"); }
            }
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Warianty nazwy
    // ─────────────────────────────────────────────────────────────────────────
    private static IEnumerable<string> BuildNameVariants(string name)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void A(string s) { if (!string.IsNullOrWhiteSpace(s)) set.Add(s.Trim()); }

        A(name);
        A(name.Replace(" ", ""));
        A(name.Replace(" ", "-"));
        A(name.Replace(" ", "_"));
        A(name.ToLower());
        A(name.ToLower().Replace(" ", ""));
        A(name.ToLower().Replace(" ", "-"));
        A(name.ToLower().Replace(" ", "_"));
        A(name.ToUpper());
        A(name.ToUpper().Replace(" ", ""));



        return set;
    }

    private static string Slug(string s) =>
        s.ToLower().Replace(" ", "-");

    // ─────────────────────────────────────────────────────────────────────────
    //  Rozmiar katalogu
    // ─────────────────────────────────────────────────────────────────────────
    private static long GetDirectorySize(string path, CancellationToken ct)
    {
        long total = 0;
        try
        {
            var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
            
            var enumerable = new System.IO.Enumeration.FileSystemEnumerable<long>(
                path,
                (ref System.IO.Enumeration.FileSystemEntry entry) => entry.Length,
                options)
            {
                ShouldIncludePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => !entry.IsDirectory
            };

            foreach (var size in enumerable)
            {
                ct.ThrowIfCancellationRequested();
                total += size;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { AppLogger.Log($"[GetDirectorySize Pętla]: {ex.ToString()}"); }
        return total;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Usuwanie
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task<DeleteResult> DeleteAsync(
        IEnumerable<FoundItem> items,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(async () =>
        {
            int ok = 0, errors = 0;
            var errorMessages = new List<string>();
            var sudoPaths = new List<string>();

            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    progress?.Report(item.ShortPath);
                    if (!IsPathSafeToDelete(item.Path))
                    {
                        AppLogger.Log($"[ZABEZPIECZENIE] Odmowa usunięcia krytycznej ścieżki (Native): '{item.Path}'");
                        errorMessages.Add($"[Zabezpieczenie] Odmowa usunięcia krytycznej ścieżki: {item.Path}");
                        errors++;
                        continue;
                    }

                    if (item.IsDirectory)
                        Directory.Delete(item.Path, recursive: true);
                    else
                        File.Delete(item.Path);
                    ok++;
                }
                catch (UnauthorizedAccessException)
                {
                    sudoPaths.Add(item.Path);
                }
                catch (DirectoryNotFoundException ex) { ok++; AppLogger.Log($"[DeleteAsync Brak ścieżki]: {ex.ToString()}"); }
                catch (FileNotFoundException ex)      { ok++; AppLogger.Log($"[DeleteAsync Brak pliku]: {ex.ToString()}"); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    errors++;
                    errorMessages.Add($"{item.ShortPath}: {ex.Message}");
                    AppLogger.Log($"[DeleteAsync błąd na pliku]: {ex.ToString()}");
                }
            }

            if (sudoPaths.Count > 0)
            {
                try
                {
                    progress?.Report("Wymagane uprawnienia administratora (sprawdź monit systemowy)...");
                    var bashLines = new List<string>();
                    var rmPaths = new List<string>();

                    foreach (var path in sudoPaths)
                    {
                        if (path.Contains("/var/db/receipts/", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("/private/var/db/receipts/", StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName = Path.GetFileName(path);
                            if (fileName.EndsWith(".bom") || fileName.EndsWith(".plist"))
                            {
                                string ext = fileName.EndsWith(".bom") ? ".bom" : ".plist";
                                string pkgId = fileName.Substring(0, fileName.Length - ext.Length);
                                
                                bashLines.Add($"pkgutil --forget '{BashHelpers.Escape(pkgId)}' || true");
                            }
                        }
                        
                        rmPaths.Add(path);
                    }

                    foreach (var p in rmPaths)
                    {
                        if (!IsPathSafeToDelete(p))
                        {
                            AppLogger.Log($"[ZABEZPIECZENIE] Odmowa wygenerowania komendy rm -rf dla ścieżki: '{p}' (krytyczna dla systemu)");
                            errorMessages.Add($"[Zabezpieczenie] Odmowa usunięcia krytycznej ścieżki: {p}");
                            errors++;
                            continue;
                        }
                        
                        bashLines.Add($"rm -rf '{BashHelpers.Escape(p)}'");
                    }

                    if (bashLines.Count == 0)
                    {
                        // Wszystkie ścieżki sudo zostały odrzucone przez blacklist
                    }
                    else
                    {
                        int actualSudoCommands = bashLines.Count;
                        
                        string tempScriptFile = Path.Combine(Path.GetTempPath(), $"vstdeleter_del_{Guid.NewGuid():N}.sh");
                        string tempFlagFile = Path.Combine(Path.GetTempPath(), $"vstdeleter_del_{Guid.NewGuid():N}.flag");
                        try
                        {
                            await File.WriteAllTextAsync(tempFlagFile, "1", ct);
                            var scriptLines = new List<string> { "#!/bin/bash", "SUCC=0", "ERR=0" };
                            foreach (var line in bashLines)
                            {
                                scriptLines.Add($"[ ! -f '{BashHelpers.Escape(tempFlagFile)}' ] && exit 1");
                                scriptLines.Add($"{line} && SUCC=$((SUCC+1)) || ERR=$((ERR+1))");
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
                                        errorMessages.Add($"Sudo częściowo zablokowane. Pomyślnie: {parsedOk}, Błędy: {parsedErr}");
                                    }
                                }
                                else
                                {
                                    errors += actualSudoCommands;
                                    errorMessages.Add($"Odmowa dostępu / błąd sudo: {errStr.Trim()} {outStr.Trim()}");
                                }
                            }
                            else
                            {
                                errors += actualSudoCommands;
                                errorMessages.Add("Nie udało się uruchomić osascript.");
                            }
                        }
                        finally
                        {
                            try { if (File.Exists(tempScriptFile)) File.Delete(tempScriptFile); } catch { }
                            try { if (File.Exists(tempFlagFile)) File.Delete(tempFlagFile); } catch { }
                        }
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    errors += sudoPaths.Count;
                    errorMessages.Add($"Brak programu osascript w systemie: {ex.Message}");
                    AppLogger.Log($"[delete sudo] osascript missing: {ex.ToString()}");
                }
                catch (InvalidOperationException ex)
                {
                    errors += sudoPaths.Count;
                    errorMessages.Add($"Błąd procesu sudo: {ex.Message}");
                    AppLogger.Log($"[delete sudo] process error: {ex.ToString()}");
                }
                catch (Exception ex)
                {
                    errors += sudoPaths.Count;
                    errorMessages.Add($"Nieoczekiwany błąd podczas próby sudo: {ex.Message}");
                    AppLogger.Log($"[delete sudo] unexpected: {ex.ToString()}");
                }
            }

            return new DeleteResult(ok, errors, errorMessages);
        }, ct);
    }

    public static bool IsPathSafeToDelete(string p)
    {
        string safePath = p.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(safePath) || safePath.Length < 12) return false;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string[] criticalDirectories = {
            "/Library", "/System", "/Users", "/Applications", "/private", "/bin", "/sbin", "/usr", "/Volumes", "/Network",
            "/Library/Application Support", "/Library/Preferences", "/Library/Caches", "/Library/Logs",
            "/Library/Audio", "/Library/Audio/Plug-Ins", "/Library/Audio/Plug-Ins/VST",
            "/Library/Audio/Plug-Ins/VST3", "/Library/Audio/Plug-Ins/Components",
            "/Library/Audio/Plug-Ins/CLAP", "/Library/Audio/Plug-Ins/HAL",
            "/Library/Audio/Plug-Ins/WPAPI", "/Library/Audio/Plug-Ins/MAS",
            "/Library/Application Support/Avid", "/Library/Application Support/Avid/Audio", 
            "/Library/Application Support/Avid/Audio/Plug-Ins",
            "/Library/Application Support/Apple", "/Library/Application Support/CrashReporter",
            "/Users/Shared",
            home,
            $"{home}/Library",
            $"{home}/Library/Application Support",
            $"{home}/Library/Application Support/Apple",
            $"{home}/Library/Application Support/CrashReporter",
            $"{home}/Library/Preferences",
            $"{home}/Library/Caches",
            $"{home}/Library/Logs",
            $"{home}/Library/Audio",
            $"{home}/Documents",
            $"{home}/Downloads",
            $"{home}/Desktop",
            $"{home}/Pictures",
            $"{home}/Music",
            $"{home}/Movies"
        };

        foreach (var dir in criticalDirectories)
        {
            if (safePath.Equals(dir, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }
}

public record DeleteResult(int Deleted, int Errors, List<string> ErrorMessages);
