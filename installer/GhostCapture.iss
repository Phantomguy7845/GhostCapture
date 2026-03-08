#ifndef SourceDir
  #define SourceDir "..\artifacts\publish\win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{8A42E58E-22D1-4BA1-A21A-7B8DD4B72D1C}
AppName=GhostCapture
AppVersion={#AppVersion}
AppPublisher=GhostCapture
DefaultDirName={autopf}\GhostCapture
DefaultGroupName=GhostCapture
UninstallDisplayIcon={app}\GhostCapture.exe
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
OutputDir={#OutputDir}
OutputBaseFilename=GhostCapture-Setup
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\GhostCapture"; Filename: "{app}\GhostCapture.exe"
Name: "{autodesktop}\GhostCapture"; Filename: "{app}\GhostCapture.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\GhostCapture.exe"; Description: "Launch GhostCapture"; Flags: nowait postinstall skipifsilent

