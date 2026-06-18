using VstDeleter.Models;

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
            string searchTerm = norm.ToLower();
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
                            fileName.StartsWith("com.microsoft."))
                        {
                            continue;
                        }

                        if (fileName.Contains(searchTerm))
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
            foreach (string f in Directory.EnumerateFiles(path, "*", options))
            {
                ct.ThrowIfCancellationRequested();
                try { total += new FileInfo(f).Length; } 
                catch (Exception ex) { AppLogger.Log($"[GetDirectorySize]: {ex.ToString()}"); }
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
                                
                                bashLines.Add($"pkgutil --forget '{pkgId.Replace("'", "'\\''")}' || true");
                            }
                        }
                        else
                        {
                            rmPaths.Add(path);
                        }
                    }

                    foreach (var p in rmPaths)
                    {
                        bashLines.Add($"rm -rf '{p.Replace("'", "'\\''")}'");
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
                                ok += sudoPaths.Count;
                            }
                            else
                            {
                                errors += sudoPaths.Count;
                                errorMessages.Add($"Odmowa dostępu / błąd sudo: {err.Trim()}");
                            }
                        }
                        else
                        {
                            errors += sudoPaths.Count;
                            errorMessages.Add("Nie udało się uruchomić osascript.");
                        }
                    }
                    finally
                    {
                        if (File.Exists(scriptPath)) File.Delete(scriptPath);
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
}

public record DeleteResult(int Deleted, int Errors, List<string> ErrorMessages);
