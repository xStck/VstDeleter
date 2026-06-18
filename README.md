# VST Deleter

VST Deleter is a powerful, GUI-based utility designed for macOS (Apple Silicon) to safely and thoroughly remove orphaned audio plugins and their leftover files. Built with C# and Avalonia UI, it provides deep scanning, backup creation, and elevated privileges (Sudo) for stubborn system files.

## Key Features

- **Deep Scanning:** Scans for VST, VST3, AU (AudioUnit), CLAP, and AAX formats, not just in standard plugin folders, but also recursively up to 2 levels deep in `Application Support`, `Presets`, and `/Users/Shared`.
- **Zero-Trust Architecture:** Automatically flags only confirmed executable plugin files for deletion. All associated preferences, presets, and cache files are listed but remain unchecked by default, ensuring no third-party data is accidentally lost.
- **Built-in Backup & Restore:** Safely backs up files to a chosen directory before deletion. Supports intelligent restoration with System Integrity Protection (SIP) awareness—it will automatically ignore Apple-protected `/var/db/receipts` and properly assign `root:wheel` permissions to system files, and `user:staff` to local files.
- **Safe Root Privileges:** Uses a temporary shell script architecture to bypass macOS `ARG_MAX` shell limits and safely execute batch deletions with administrator privileges (via `osascript`), completely mitigating AppleScript Command Injection vulnerabilities.
- **High Performance UI:** Fully implements MVVM Bulk Update patterns and robust `CancellationToken` handling to ensure the UI remains smooth and responsive even when processing tens of thousands of preset files.

---

## How to Build a Standalone macOS `.app` Bundle

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
   ```bash
   cat <<EOF > "$APP_BUNDLE/Contents/Info.plist"
   <?xml version="1.0" encoding="UTF-8"?>
   <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
   <plist version="1.0">
   <dict>
       <key>CFBundleName</key>
       <string>VstDeleter</string>
       <key>CFBundleDisplayName</key>
       <string>VST Deleter</string>
       <key>CFBundleIdentifier</key>
       <string>com.yourdomain.vstdeleter</string>
       <key>CFBundleVersion</key>
       <string>1.0.0</string>
       <key>CFBundlePackageType</key>
       <string>APPL</string>
       <key>CFBundleSignature</key>
       <string>????</string>
       <key>CFBundleExecutable</key>
       <string>VstDeleter</string>
       <key>CFBundleIconFile</key>
       <string>Icon.icns</string>
       <key>LSMinimumSystemVersion</key>
       <string>11.0</string>
       <key>NSPrincipalClass</key>
       <string>NSApplication</string>
       <key>NSHighResolutionCapable</key>
       <true/>
   </dict>
   </plist>
   EOF
   ```

6. *(Optional)* Add an icon:
   If you have a macOS icon file (`.icns`), place it inside the `Resources` folder and name it `Icon.icns`:
   ```bash
   cp /path/to/your/icon.icns "$RESOURCES_DIR/Icon.icns"
   ```

7. **Done!** You now have a working `VstDeleter.app` bundle in your directory. You can double-click it to run, or move it to your `/Applications` folder.
