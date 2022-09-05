[Setup]
AppName=flowOSD
AppVersion=1.1.0
AppVerName=flowOSD 1.1.0
AppCopyright=© 2001, Albert Akhmetov
WizardStyle=modern
DefaultDirName={autopf}\flowOSD
UninstallDisplayIcon={app}\flowOSD.exe
Compression=lzma2/max
SolidCompression=yes
OutputDir=..\output
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
LicenseFile=..\LICENSE

AppPublisher=Albert Akhmetov
AppPublisherURL=https://albertakhmetov.com

AppSupportURL=https://github.com/albertakhmetov/flowOSD

VersionInfoVersion=1.1.0
VersionInfoProductName=flowOSD

DisableProgramGroupPage=yes
OutputBaseFilename=flowOSD-1.1.0

[Files]
Source: "..\output\publish\*.*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{autostartmenu}\flowOSD"; Filename: "{app}\flowOSD.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "flowOSD"; ValueData: "{app}\flowOSD.exe"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\flowOSD.exe"; Description: "Run flowOSD"; Flags: postinstall shellexec
