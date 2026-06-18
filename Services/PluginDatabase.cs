namespace VstDeleter.Services;

/// <summary>
/// Baza znanych wtyczek VST/AU z sugestiami dla autouzupełniania.
/// </summary>
public static class PluginDatabase
{
    public static readonly IReadOnlyList<string> KnownPlugins = new[]
    {
        // ── Native Instruments ───────────────────────────────────────────────
        "Guitar Rig 6", "Guitar Rig 7",
        "Kontakt 7", "Kontakt 6", "Kontakt 5",
        "Massive X", "Massive",
        "Battery 4",
        "Reaktor 6",
        "Absynth 5",
        "FM8",
        "Razor",
        "Monark",
        "Rounds",
        "Prism",
        "Skanner XT",
        "Blocks",
        "Replika XT", "Replika",
        "Supercharger GT", "Supercharger",
        "Raum",
        "Phasis",
        "Flair",
        "Choral",
        "Solid Bus Comp", "Solid Dynamics", "Solid EQ", "Solid Limiter",
        "VC 2A", "VC 76", "VC 160",
        "Transient Master",
        "Vari Comp",
        "Driver",
        "Form",
        "Retro Machines MK2",
        "Session Strings Pro 2",
        "Vintage Organs",
        "Native Access",
        "Komplete Kontrol",
        "Maschine",
        "Traktor",

        // ── Xfer Records ────────────────────────────────────────────────────
        "Serum",
        "LFO Tool",
        "Cthulhu",
        "Nerve",
        "Dimension Expander",

        // ── Spectrasonics ────────────────────────────────────────────────────
        "Omnisphere 2",
        "Trilian",
        "Keyscape",
        "Stylus RMX",

        // ── iZotope ─────────────────────────────────────────────────────────
        "Ozone 11", "Ozone 10", "Ozone 9",
        "Neutron 4", "Neutron 3",
        "RX 10", "RX 9", "RX 8",
        "Nectar 3",
        "Iris 2",
        "Trash 2",
        "Stutter Edit 2",
        "BreakTweaker",
        "VocalSynth 2",
        "Spire Studio",
        "Relay",
        "Imager",
        "Dialogue Match",
        "Exponential Audio R4",

        // ── FabFilter ───────────────────────────────────────────────────────
        "FabFilter Pro-Q 3", "FabFilter Pro-Q 4",
        "FabFilter Pro-MB",
        "FabFilter Saturn 2",
        "FabFilter Twin 3",
        "FabFilter Timeless 3",
        "FabFilter Volcano 3",
        "FabFilter Pro-R 2",
        "FabFilter Pro-G",
        "FabFilter Pro-C 2",
        "FabFilter Pro-DS",
        "FabFilter Pro-L 2",
        "FabFilter Micro",
        "FabFilter One",
        "FabFilter Simplon",

        // ── Arturia ──────────────────────────────────────────────────────────
        "Arturia V Collection",
        "Arturia Analog Lab",
        "Arturia Mini V3",
        "Arturia Matrix-12 V2",
        "Arturia Wurli V3",
        "Arturia Stage-73 V2",
        "Arturia Farfisa V2",
        "Arturia Solina V2",
        "Arturia Jup-8 V4",
        "Arturia SEM V2",
        "Arturia OB-Xa V3",
        "Arturia Prophet-5 V2",
        "Arturia CS-80 V3",
        "Arturia ARP 2600 V3",
        "Arturia Synclavier V",
        "Arturia Buchla Easel V",
        "Arturia DX7 V",
        "Arturia CMI V",
        "Arturia Vocoder V",
        "Arturia Pigments",
        "Arturia MiniFreak V",
        "Arturia ACID V",
        "Arturia Korg MS-20 V",

        // ── Waves ────────────────────────────────────────────────────────────
        "Waves Central",
        "Waves SSL 4000 Collection",
        "Waves CLA-2A",
        "Waves CLA-3A",
        "Waves CLA-76",
        "Waves H-Delay",
        "Waves H-Reverb",
        "Waves Abbey Road",
        "Waves L3-16 Multimaximizer",
        "Waves Vocal Rider",
        "Waves Tune Real-Time",
        "Waves NS1 Noise Suppressor",
        "Waves OVox",
        "Waves Silk Vocals",
        "Waves Butch Vig",
        "Waves Eddie Kramer",
        "Waves Tony Maserati",
        "Waves Renaissance EQ",
        "Waves API Collection",

        // ── SoundToys ────────────────────────────────────────────────────────
        "SoundToys EchoBoy",
        "SoundToys Decapitator",
        "SoundToys PrimalTap",
        "SoundToys MicroShift",
        "SoundToys Crystallizer",
        "SoundToys Effectrix",
        "SoundToys FilterFreak",
        "SoundToys PhaseMistress",
        "SoundToys Tremolator",
        "SoundToys Radiator",
        "SoundToys Little AlterBoy",
        "SoundToys Little Plate",
        "SoundToys Devil-Loc",
        "SoundToys Rack",
        "SoundToys Panman",
        "SoundToys 5",

        // ── Valhalla DSP ─────────────────────────────────────────────────────
        "Valhalla Room",
        "Valhalla VintageVerb",
        "Valhalla Delay",
        "Valhalla Shimmer",
        "Valhalla Plate",
        "Valhalla Supermassive",
        "Valhalla SpaceModulator",

        // ── UAD / Universal Audio ────────────────────────────────────────────
        "UAD Console",
        "UAD Oxide Tape",
        "UAD Neve 1073",
        "UAD API 2500",
        "UAD Empirical Labs FATSO",
        "UAD Century Tube Channel Strip",
        "UAD 1176 Classic Limiter",
        "UAD LA-2A Classic Limiter",
        "UAD Pultec EQP-1A",
        "UAD Ampex ATR-102",

        // ── Slate Digital ────────────────────────────────────────────────────
        "Slate Digital VCC",
        "Slate Digital VTM",
        "Slate Digital Virtual Mix Rack",
        "Slate Digital Fresh Air",
        "Slate Digital Custom Series",
        "Slate Digital Alchemy",
        "Slate Digital Trigger 2",
        "Slate Digital Dragon",

        // ── Eventide ─────────────────────────────────────────────────────────
        "Eventide H3000 Factory",
        "Eventide H910 Harmonizer",
        "Eventide UltraChannel",
        "Eventide Fission",
        "Eventide Blackhole",
        "Eventide Physion",
        "Eventide EChannel",
        "Eventide 2016 Stereo Room",

        // ── Antares ──────────────────────────────────────────────────────────
        "Antares Auto-Tune Pro",
        "Antares Auto-Tune Realtime Advanced",
        "Antares AVOX",
        "Antares Harmony Engine",
        "Antares Articulator",
        "Antares Choir",

        // ── Celemony ─────────────────────────────────────────────────────────
        "Melodyne 5 Studio",
        "Melodyne 5 Editor",
        "Melodyne 5 Essential",

        // ── Plugin Alliance ──────────────────────────────────────────────────
        "ADPTR STREAMLINER",
        "Brainworx bx_console",
        "Vertigo VSC-2",
        "Black Box Analog Design HG-2",
        "Unfiltered Audio Sandman Pro",

        // ── Spitfire Audio ───────────────────────────────────────────────────
        "Spitfire LABS",
        "Spitfire BBC Symphony Orchestra",
        "Spitfire BBCSO Discover",
        "Spitfire Albion One",

        // ── Output ───────────────────────────────────────────────────────────
        "Output Arcade",
        "Output Portal",
        "Output Exhaust",
        "Output Movement",
        "Output Signal",
        "Output Substance",

        // ── Steinberg ────────────────────────────────────────────────────────
        "HALion",
        "HALion Sonic",
        "Groove Agent",
        "Padshop",
        "Retrologue",
        "Mystic",
        "Spector",
        "Embracer",
        "REVerence",
        "VST Connect",

        // ── Misc / Popular ────────────────────────────────────────────────────
        "Sausage Fattener",
        "Kickstart",
        "Cableguys ShaperBox",
        "Cableguys HalfTime",
        "Cableguys Volume Shaper",
        "Cableguys Pancake",
        "Baby Audio Smooth Operator",
        "Baby Audio Transit",
        "Baby Audio Super VHS",
        "Baby Audio IHNY-2",
        "Vital",
        "Surge XT",
        "BBCSO Discover",
        "AudioThing Springs",
        "AudioThing Wires",
        "Kilohearts Toolbox",
        "Kilohearts Phase Plant",
        "Kilohearts Snap Heap",
        "Initial Audio Sektor",
        "Tone2 Icarus",
        "u-he Diva",
        "u-he Hive 2",
        "u-he Zebra 2",
        "u-he Repro-1",
        "u-he Repro-5",
        "u-he ACE",
        "u-he Bazille",
        "u-he Podolski",
        "u-he Tyrell N6",
        "Reveal Sound Spire",
        "Rob Papen Predator 3",
        "Rob Papen Blue-II",
        "Rob Papen BLADE-2",
        "Rob Papen Go2",
        "Lennar Digital Sylenth1",
        "D16 Group LuSH-101",
        "D16 Group Drumazon",
        "D16 Group Nepheton",
        "D16 Group Nithonat",
        "D16 Group Phoscyon",
        "D16 Group Tekturon",
        "D16 Group PunchBOX",
        "D16 Group Decimort 2",
        "D16 Group Frontier",
        "D16 Group Toraverb 2",
        "Cymatics Diablo",
        "KSHMR Karma",
        "Loopmasters Loopcloud",
        "Roland Cloud",
        "Korg Collection",
        "Moog Subsequent",
        "Moog Model D",
    };

    public static readonly IReadOnlyList<string> KnownPluginsNormalized = KnownPlugins.Select(p => p.Replace(" ", "")).ToArray();

    /// <summary>
    /// Zwraca dopasowania do frazy wyszukiwania (max <paramref name="maxResults"/>).
    /// </summary>
    public static IEnumerable<string> Search(string query, int maxResults = 8)
    {
        if (string.IsNullOrWhiteSpace(query)) yield break;

        string q = query.Trim().Replace(" ", "");
        int count = 0;

        // Najpierw te zaczynające się od frazy
        for (int i = 0; i < KnownPlugins.Count; i++)
        {
            if (KnownPluginsNormalized[i].StartsWith(q, StringComparison.OrdinalIgnoreCase))
            {
                yield return KnownPlugins[i];
                if (++count >= maxResults) yield break;
            }
        }

        // Potem zawierające frazę
        for (int i = 0; i < KnownPlugins.Count; i++)
        {
            if (!KnownPluginsNormalized[i].StartsWith(q, StringComparison.OrdinalIgnoreCase)
                && KnownPluginsNormalized[i].Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                yield return KnownPlugins[i];
                if (++count >= maxResults) yield break;
            }
        }
    }
}
