<div align="center">
  <img src="https://img.icons8.com/color/128/000000/mac-os.png" alt="macOS Logo" width="80" />
  <h1>VST Deleter</h1>
  <p><strong>A robust, deeply-integrated uninstaller for Audio Plugins on macOS.</strong></p>

  <p>
    <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 8" />
    <img src="https://img.shields.io/badge/Platform-macOS_Apple_Silicon-000000?logo=apple&logoColor=white" alt="macOS" />
    <img src="https://img.shields.io/badge/UI-Avalonia-8A2BE2" alt="Avalonia UI" />
    <img src="https://img.shields.io/badge/Paradigm-Vibe_Coding-FF9900" alt="Vibe Coding" />
  </p>
</div>

---

## 🎵 Overview

**VST Deleter** is a powerful, GUI-based utility designed exclusively for macOS (Apple Silicon & Intel). Its primary goal is to safely and thoroughly track down orphaned audio plugins, their presets, caches, and leftover preference files, providing users with a clean, 1-click uninstallation or backup process.

Built with C# and the Avalonia UI framework, it provides deep scanning capabilities and robust privilege escalation (Sudo) for stubborn system files, without sacrificing safety or performance.

---

## 🤖 The "Vibe Coding" Philosophy

This application is proudly the result of **Vibe Coding** — an AI-assisted, intent-driven development paradigm. 

Created initially for personal needs to solve the notorious macOS audio-plugin clutter, this project was architected, written, and rigorously security-audited through a continuous pairing session with an Advanced Agentic AI. 

Rather than manually writing boilerplate, the focus was placed entirely on **architectural design, security constraints, and user experience (the "vibe")**. The AI handled the heavy lifting of C# MVVM patterns, parallel I/O optimizations, and UNIX-level bash injection mitigations, proving that high-quality, production-ready system utilities can be forged through high-level conceptual guidance.

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


