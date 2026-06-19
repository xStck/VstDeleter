<div align="center">
  <img src="https://img.icons8.com/color/128/000000/mac-os.png" alt="macOS Logo" width="80" />
  <h1>VST Deleter</h1>
  <p><strong>A robust, deeply-integrated uninstaller for Audio Plugins on macOS.</strong></p>

  <p>
    <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 8" />
    <img src="https://img.shields.io/badge/Platform-macOS_Apple_Silicon-000000?logo=apple&logoColor=white" alt="macOS" />
    <img src="https://img.shields.io/badge/UI-Avalonia-8A2BE2" alt="Avalonia UI" />
  </p>
</div>

---

## 🎵 Overview

**VST Deleter** is a powerful, GUI-based utility designed exclusively for macOS (Apple Silicon & Intel). Its primary goal is to safely and thoroughly track down orphaned audio plugins, their presets, caches, and leftover preference files, providing users with a clean, 1-click uninstallation or backup process.

Built with C# and the Avalonia UI framework, it provides deep scanning capabilities and robust privilege escalation (Sudo) for stubborn system files, without sacrificing safety or performance.

---

## 🤖 AI-Assisted Development

This application was created with the assistance of an Advanced Agentic AI. The AI handled the implementation, including C# MVVM patterns, parallel I/O, and UNIX bash scripts, based on the user's requirements.

---

## ✨ Key Features

- 🔍 **Deep System Scanning:** Tracks down VST, VST3, AU (AudioUnit), CLAP, and AAX formats. Recursively scans up to 2 levels deep in hidden directories like `~/Library/Application Support`, `/Library/Audio/Presets`, and `/Users/Shared`.
- 🛡 **Zero-Trust Architecture:** Operates on a strict blacklist/whitelist system. Critical macOS directories (`/Library`, `/System`, `~/Documents`) are hard-coded into an `IsPathSafeToDelete` gatekeeper to prevent catastrophic data loss, even if config files are manipulated.
- 📦 **Built-in Backup & Restore:** Safely backs up files to a compressed JSON manifest directory before deletion. Supports intelligent restoration with System Integrity Protection (SIP) awareness—automatically reassigning `root:wheel` permissions to system files and `user:staff` to local files.
- 🔐 **Safe Root Privileges:** Uses a temporary shell script architecture via `osascript` to safely execute batch deletions with administrator privileges. All inputs are double-escaped to completely mitigate AppleScript and Bash Command Injection vulnerabilities.
- ⚡ **High-Performance UI:** Fully implements MVVM Bulk Update patterns, `FileSystemEnumerable` for zero-allocation directory sizing, and robust `CancellationToken` flows to ensure the UI remains buttery-smooth even when crunching tens of thousands of plugin assets.

---

## 🛠 How to Build a Standalone macOS `.app` Bundle

To distribute or use VST Deleter as a native macOS application, you need to compile it and package the output into an `.app` bundle structure.

### Prerequisites
- macOS operating system
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

### Automated Build (Recommended)
You can build the app instantly using the included bash script:

1. Open a terminal and navigate to the project directory:
   ```bash
   cd /path/to/VstDeleter
   ```
2. Run the build script:
   ```bash
   ./build_mac.sh
   ```
The script will automatically compile the project for Apple Silicon, generate the required `Info.plist`, set executable permissions, and output a ready-to-use `VstDeleter.app` inside the `dist/` directory.

### Manual Build Instructions
If you prefer to build the app manually:

1. Open a terminal and navigate to the root directory of this repository:
   ```bash
   cd /path/to/VstDeleter
   ```

2. Create the `.app` bundle directory structure:
   ```bash
   APP_NAME="VstDeleter"
   APP_BUNDLE="$APP_NAME.app"
   MAC_OS_DIR="$APP_BUNDLE/Contents/MacOS"
   RESOURCES_DIR="$APP_BUNDLE/Contents/Resources"

   mkdir -p "$MAC_OS_DIR"
   mkdir -p "$RESOURCES_DIR"
   ```

3. Publish the .NET application as a self-contained executable for Apple Silicon (`osx-arm64`):
   ```bash
   dotnet publish -c Release -r osx-arm64 --self-contained true -o "$MAC_OS_DIR"
   ```
   *(If you are building for older Intel Macs, change `-r osx-arm64` to `-r osx-x64`)*

4. Set executable permissions on the binary:
   ```bash
   chmod +x "$MAC_OS_DIR/$APP_NAME"
   ```

5. Create the required `Info.plist` file inside the `Contents` directory:
   *(See the build script or previous documentation for the full XML payload)*

6. *(Optional)* Add an icon:
   If you have a macOS icon file (`.icns`), place it inside the `Resources` folder and name it `Icon.icns`:
   ```bash
   cp /path/to/your/icon.icns "$RESOURCES_DIR/Icon.icns"
   ```

7. **Done!** You now have a working `VstDeleter.app` bundle in your directory. You can double-click it to run, or move it to your `/Applications` folder.

---

## ⚠️ Post-Uninstall (DAW Restart)

macOS and many modern DAWs (like Ableton Live, Logic Pro, FL Studio) use internal caching for Audio Units (AU) and VST plugins.

**VST Deleter automatically clears the macOS CoreAudio cache** and forces the background system daemons to reset their databases after successful uninstallation. 
You no longer need to perform manual deep rescans or reboot your Mac to get rid of ghost plugins! Simply **restart your DAW** after using VST Deleter, and the removed plugins will completely disappear from your browser.

---
<div align="center">
  <img src="https://img.icons8.com/color/128/000000/mac-os.png" alt="macOS Logo" width="80" />
  <h1>VST Deleter (Wersja Polska)</h1>
  <p><strong>Potężny, głęboko zintegrowany deinstalator wtyczek Audio na system macOS.</strong></p>
</div>

---

## 🎵 Przegląd

**VST Deleter** to zaawansowane narzędzie z interfejsem graficznym, zaprojektowane wyłącznie dla macOS (Apple Silicon oraz Intel). Jego głównym celem jest bezpieczne i niezwykle dokładne namierzanie osieroconych wtyczek audio, ich presetów, pamięci podręcznych (cache) oraz pozostawionych plików konfiguracyjnych, zapewniając użytkownikom czysty proces odinstalowania lub tworzenia kopii zapasowej za pomocą 1 kliknięcia.

Narzędzie zbudowane w C# i oparte na bibliotece Avalonia UI zapewnia głębokie skanowanie i solidne podnoszenie uprawnień (Sudo) dla uporczywych plików systemowych, bez utraty bezpieczeństwa czy wydajności.

---

## 🤖 Rozwój wspierany przez AI

Aplikacja została napisana z pomocą zaawansowanej sztucznej inteligencji (Advanced Agentic AI). Sztuczna inteligencja zajęła się implementacją całego kodu, w tym wzorców C# MVVM, równoległych operacji wejścia/wyjścia na dysku oraz skryptów bash systemu UNIX, w oparciu o wytyczne użytkownika.

---

## ✨ Główne funkcje

- 🔍 **Głębokie skanowanie systemu:** Śledzi formaty VST, VST3, AU (AudioUnit), CLAP oraz AAX. Skanuje rekursywnie do 2 poziomów w głąb w ukrytych folderach, takich jak `~/Library/Application Support`, `/Library/Audio/Presets` czy `/Users/Shared`.
- 🛡 **Architektura Zero-Trust:** Działa w oparciu o rygorystyczny system czarnych i białych list. Krytyczne katalogi macOS (`/Library`, `/System`, `~/Documents`) są zaszyte na sztywno w tzw. bramkarzu `IsPathSafeToDelete`, zapobiegając katastrofalnej utracie danych, nawet w przypadku celowej manipulacji przy plikach konfiguracyjnych.
- 📦 **Wbudowana kopia zapasowa i przywracanie:** Bezpiecznie robi kopię plików do spakowanego folderu z manifestem JSON przed jakimkolwiek usunięciem. Obsługuje inteligentne przywracanie, z uwzględnieniem System Integrity Protection (SIP) — automatycznie nadając uprawnienia `root:wheel` plikom systemowym oraz `user:staff` plikom lokalnym użytkownika.
- 🔐 **Bezpieczne uprawnienia Root:** Używa tymczasowej architektury skryptów powłoki (bash) poprzez `osascript` by bezpiecznie wykonać grupowe usuwanie plików z uprawnieniami administratora. Wszystkie dane wejściowe są podwójnie "eskejpowane" (odbezpieczane), aby całkowicie zniwelować ryzyko podatności AppleScript i Bash Command Injection.
- ⚡ **Wysokowydajny interfejs:** W pełni wykorzystuje wzorce MVVM Bulk Update, `FileSystemEnumerable` w celu sprawdzenia rozmiaru folderów z zerową alokacją w pamięci RAM, oraz solidne procesy obsługi `CancellationToken`, aby interfejs użytkownika pozostał ultra-płynny nawet podczas przetwarzania dziesiątek tysięcy zasobów wtyczek.

---

## 🛠 Jak zbudować natywny pakiet `.app` na macOS

Aby udostępnić lub używać VST Deleter jako natywnej aplikacji macOS, musisz ją skompilować i spakować produkt końcowy do struktury folderu `.app`.

### Wymagania wstępne
- System operacyjny macOS
- Zainstalowane środowisko [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### Zautomatyzowane budowanie (Zalecane)
Możesz zbudować aplikację natychmiast, używając dołączonego skryptu bash:

1. Otwórz terminal i przejdź do katalogu projektu:
   ```bash
   cd /path/to/VstDeleter
   ```
2. Uruchom skrypt budujący:
   ```bash
   ./build_mac.sh
   ```
Skrypt automatycznie skompiluje projekt dla architektury Apple Silicon, wygeneruje wymagany plik `Info.plist`, ustawi uprawnienia do wykonywania oraz umieści gotową do użycia aplikację `VstDeleter.app` wewnątrz folderu `dist/`.

### Instrukcja ręcznego budowania
Jeśli wolisz zbudować aplikację ręcznie:

1. Otwórz terminal i przejdź do katalogu głównego tego repozytorium:
   ```bash
   cd /path/to/VstDeleter
   ```

2. Utwórz strukturę katalogów pakietu `.app`:
   ```bash
   APP_NAME="VstDeleter"
   APP_BUNDLE="$APP_NAME.app"
   MAC_OS_DIR="$APP_BUNDLE/Contents/MacOS"
   RESOURCES_DIR="$APP_BUNDLE/Contents/Resources"

   mkdir -p "$MAC_OS_DIR"
   mkdir -p "$RESOURCES_DIR"
   ```

3. Opublikuj aplikację .NET jako samodzielny plik wykonywalny dla Apple Silicon (`osx-arm64`):
   ```bash
   dotnet publish -c Release -r osx-arm64 --self-contained true -o "$MAC_OS_DIR"
   ```
   *(Jeśli budujesz na starsze Maci z procesorami Intel, zmień `-r osx-arm64` na `-r osx-x64`)*

4. Ustaw prawa wykonywania na wygenerowanym pliku:
   ```bash
   chmod +x "$MAC_OS_DIR/$APP_NAME"
   ```

5. Stwórz niezbędny plik `Info.plist` wewnątrz folderu `Contents`:
   *(Zobacz zawartość skryptu budującego lub poprzednią dokumentację, by pobrać wymagany payload XML)*

6. *(Opcjonalne)* Dodaj ikonę:
   Jeśli posiadasz plik ikony macOS (`.icns`), umieść go wewnątrz folderu `Resources` i nazwij go `Icon.icns`:
   ```bash
   cp /path/to/your/icon.icns "$RESOURCES_DIR/Icon.icns"
   ```

7. **Gotowe!** Posiadasz teraz spakowany, działający folder aplikacji `VstDeleter.app`. Możesz kliknąć na niego dwukrotnie, aby go uruchomić, lub przenieść do folderu `/Applications`.

---

## ⚠️ Po usunięciu (Odświeżanie DAW)

macOS i wiele współczesnych programów DAW (jak Ableton Live, Logic Pro, FL Studio) korzysta z wewnętrznej pamięci podręcznej (cache) dla wtyczek Audio Units (AU) oraz VST.

**VST Deleter automatycznie czyści systemową pamięć podręczną CoreAudio** i resetuje procesy w tle odpowiadające za indeksowanie wtyczek na Macu natychmiast po udanym procesie usuwania.
Nie musisz już wykonywać ręcznych, głębokich skanowań w opcjach ani restartować komputera! Po prostu **zrestartuj swój DAW** po użyciu VST Deletera, a usunięte (widmowe) wtyczki całkowicie znikną z Twojej przeglądarki.
