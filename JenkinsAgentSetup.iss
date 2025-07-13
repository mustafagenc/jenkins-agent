[Setup]
AppName=Jenkins Agent
AppVersion=1.0.0
AppPublisher=Mustafa Genç
AppPublisherURL=https://mustafagenc.info
AppSupportURL=https://mustafagenc.info
AppUpdatesURL=https://mustafagenc.info
DefaultDirName={commonpf}\JenkinsAgent
DefaultGroupName=Jenkins Agent
OutputBaseFilename=JenkinsAgentSetup_{#SetupSetting("AppVersion")}
Compression=lzma
SolidCompression=yes
SetupIconFile=Resources\jenkins.ico
UninstallDisplayIcon={app}\jenkins.ico
LanguageDetectionMethod=uilanguage

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
Source: "bin\Release\net9.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Resources\jenkins.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Jenkins Agent"; Filename: "{app}\JenkinsAgent.exe"; IconFilename: "{app}\jenkins.ico"
Name: "{group}\Jenkins Agent'ı Kaldır"; Filename: "{uninstallexe}"; IconFilename: "{app}\jenkins.ico"

[Run]
Filename: "{app}\JenkinsAgent.exe"; Description: "Jenkins Agent'ı başlat"; Flags: nowait postinstall skipifsilent
