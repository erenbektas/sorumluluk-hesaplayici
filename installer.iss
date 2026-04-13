[Setup]
AppId={{A1C3D5E7-2B4F-6A8C-9E0D-1F2A3B4C5D6E}
AppName=Sorumluluk Hesaplayıcı
AppVersion=2.1.0
AppVerName=Sorumluluk Hesaplayıcı 2.1.0
AppPublisher=Y. Eren Bektaş
DefaultDirName={autopf}\SorumlulukHesaplayici
DefaultGroupName=Sorumluluk Hesaplayıcı
UninstallDisplayIcon={app}\SorumlulukHesaplama.exe
OutputDir=installer-output
OutputBaseFilename=SorumlulukHesaplayici-v2.1.0-Setup
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=src\SorumlulukHesaplama\icon.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Ek simgeler:"

[Files]
Source: "src\SorumlulukHesaplama\bin\Release\net8.0-windows\win-x64\publish\SorumlulukHesaplama.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Sorumluluk Hesaplayıcı"; Filename: "{app}\SorumlulukHesaplama.exe"
Name: "{group}\Sorumluluk Hesaplayıcı'yı Kaldır"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Sorumluluk Hesaplayıcı"; Filename: "{app}\SorumlulukHesaplama.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SorumlulukHesaplama.exe"; Description: "Sorumluluk Hesaplayıcı'yı başlat"; Flags: nowait postinstall skipifsilent
