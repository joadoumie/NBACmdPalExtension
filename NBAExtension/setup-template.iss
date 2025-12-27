;
; To use this template for a new extension:
; 1. Copy this file to your extension's project folder as "setup-template.iss"
; 2. Replace EXTENSION_NAME with your extension name (e.g., CmdPalMyExtension)
; 3. Replace DISPLAY_NAME with your extension's display name (e.g., My Extension)
; 4. Replace DEVELOPER_NAME with your name (e.g., Your Name Here)
; 5. Replace CLSID-HERE with extensions CLSID
; 6. Update the default version to match your project file

#define AppVersion "0.0.1.1"

[Setup]
AppId={{cdcbf0e4-72a6-4e91-bd54-d4fc7fc43ba8}}
AppName="NBA Command Palette Extension"
AppVersion={#AppVersion}
AppPublisher=joadoumie
DefaultDirName={autopf}\NBAExtension
OutputDir=bin\Release\installer
OutputBaseFilename=NBAExtension-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
MinVersion=10.0.19041

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\NBA Command Palette Extension"; Filename: "{app}\NBAExtension.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{cdcbf0e4-72a6-4e91-bd54-d4fc7fc43ba8}}"; ValueData: "NBAExtension"
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{cdcbf0e4-72a6-4e91-bd54-d4fc7fc43ba8}}\LocalServer32"; ValueData: "{app}\NBAExtension.exe -RegisterProcessAsComServer"
