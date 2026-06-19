#!/bin/bash

# Zatrzymanie skryptu w przypadku błędu
set -e

APP_NAME="VstDeleter"
APP_BUNDLE="dist/$APP_NAME.app"
MAC_OS_DIR="$APP_BUNDLE/Contents/MacOS"
RESOURCES_DIR="$APP_BUNDLE/Contents/Resources"

echo "🧹 Czyszczenie poprzednich buildów..."
rm -rf dist/
mkdir -p "$MAC_OS_DIR"
mkdir -p "$RESOURCES_DIR"

echo "🔨 Kompilowanie aplikacji dla Apple Silicon (osx-arm64)..."
dotnet publish -c Release -r osx-arm64 --self-contained true -o "./temp_build"

echo "📦 Przenoszenie plików do struktury .app..."
cp -R "./temp_build/"* "$MAC_OS_DIR/"
rm -rf "./temp_build"

if [ -f "Assets/icon.icns" ]; then
    cp "Assets/icon.icns" "$RESOURCES_DIR/Icon.icns"
fi

echo "🔑 Ustawianie uprawnień wykonywania..."
chmod +x "$MAC_OS_DIR/$APP_NAME"

echo "📄 Generowanie pliku Info.plist..."
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
    <string>com.vstdeleter.app</string>
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

echo "✍️ Podpisywanie aplikacji (Ad-Hoc dla Apple Silicon)..."
codesign --force --deep --sign - "$APP_BUNDLE"

echo "🔓 Usuwanie flagi kwarantanny..."
xattr -cr "$APP_BUNDLE"

echo "✅ Gotowe! Aplikacja została zbudowana w folderze: dist/$APP_NAME.app"
echo "Możesz teraz otworzyć folder 'dist' i uruchomić VST Deleter."
