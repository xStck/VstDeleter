using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VstDeleter.Models;

namespace VstDeleter.Services;

public static class InstalledPluginsDiscovery
{
    public static async Task<List<InstalledPlugin>> DiscoverPluginsAsync()
    {
        return await Task.Run(() =>
        {
            var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Definiujemy ścieżki: parę (Systemowa, Domowa), przyjazna nazwa formatu oraz fizyczne rozszerzenie.
            var pathsToScan = new List<(string Path, string Format, string Extension)>
            {
                ("/Library/Audio/Plug-Ins/VST", "VST", ".vst"),
                (Path.Combine(home, "Library/Audio/Plug-Ins/VST"), "VST", ".vst"),
                ("/Library/Audio/Plug-Ins/VST3", "VST3", ".vst3"),
                (Path.Combine(home, "Library/Audio/Plug-Ins/VST3"), "VST3", ".vst3"),
                ("/Library/Audio/Plug-Ins/Components", "AudioUnit", ".component"),
                (Path.Combine(home, "Library/Audio/Plug-Ins/Components"), "AudioUnit", ".component"),
                ("/Library/Audio/Plug-Ins/CLAP", "CLAP", ".clap"),
                (Path.Combine(home, "Library/Audio/Plug-Ins/CLAP"), "CLAP", ".clap"),
                ("/Library/Application Support/Avid/Audio/Plug-Ins", "AAX", ".aaxplugin"),
                (Path.Combine(home, "Library/Application Support/Avid/Audio/Plug-Ins"), "AAX", ".aaxplugin")
            };

            foreach (var group in pathsToScan)
            {
                if (!Directory.Exists(group.Path))
                    continue;

                try
                {
                    var options = new EnumerationOptions { IgnoreInaccessible = true };
                    foreach (var entry in Directory.EnumerateFileSystemEntries(group.Path, "*", options))
                    {
                        if (entry.EndsWith(group.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            string name = Path.GetFileNameWithoutExtension(entry);
                            
                            if (!dict.ContainsKey(name))
                                dict[name] = new HashSet<string>();
                                
                            dict[name].Add(group.Format);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"[InstalledPluginsDiscovery Błąd skanowania {group.Path}]: {ex.ToString()}");
                }
            }

            return dict
                .Select(kvp => new InstalledPlugin(kvp.Key, kvp.Value.OrderBy(x => x).ToList()))
                .OrderBy(p => p.Name)
                .ToList();
        });
    }
}
