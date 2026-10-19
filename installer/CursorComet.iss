; Cursor Comet - Inno Setup 6 script
; Build: ISCC.exe /DMyAppVersion=1.8.2 /DMyAppNumericVersion=1.8.2 installer\CursorComet.iss

#ifndef MyAppVersion
  #define MyAppVersion "1.8.2"
#endif
#ifndef MyAppNumericVersion
  #define MyAppNumericVersion "1.8.2"
#endif

[Setup]
AppId={{A6F1C3D8-2B4E-4F7A-9C1D-8E5B3A0F6D2C}
AppName=Cursor Comet
AppVersion={#MyAppVersion}
AppVerName=Cursor Comet {#MyAppVersion}
AppPublisher=cwslz
AppPublisherURL=https://github.com/cwslz/cursor-comet
AppSupportURL=https://github.com/cwslz/cursor-comet/issues
AppUpdatesURL=https://github.com/cwslz/cursor-comet/releases
AppCopyright=Copyright (C) 2026 cwslz
DefaultDirName={localappdata}\Programs\CursorComet
DefaultGroupName=Cursor Comet
DisableProgramGroupPage=yes
DisableDirPage=no
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=CursorComet-Setup
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\cursor-comet.exe
UninstallDisplayName=Cursor Comet
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
AppMutex=CursorComet_SingleInstance_7C1E
CloseApplications=force
RestartApplications=no
UsePreviousAppDir=yes
AllowNoIcons=yes
ShowLanguageDialog=auto
LanguageDetectionMethod=uilanguage
MinVersion=10.0
VersionInfoVersion={#MyAppNumericVersion}
VersionInfoTextVersion={#MyAppVersion}
VersionInfoCompany=cwslz
VersionInfoDescription=Cursor Comet Setup
VersionInfoProductName=Cursor Comet
VersionInfoProductVersion={#MyAppNumericVersion}
VersionInfoCopyright=Copyright (C) 2026 cwslz

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "zh"; MessagesFile: "ChineseSimplified.isl"

[CustomMessages]
en.AppComment=Cursor Comet - cursor trail effect
zh.AppComment=Cursor Comet - 鼠标拖尾特效
en.AutoStart=Start with Windows
zh.AutoStart=开机自动启动

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStart}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\dist\cursor-comet.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Icons]
Name: "{userprograms}\Cursor Comet"; Filename: "{app}\cursor-comet.exe"; Parameters: "--settings"; WorkingDir: "{app}"; Comment: "{cm:AppComment}"
Name: "{autodesktop}\Cursor Comet"; Filename: "{app}\cursor-comet.exe"; Parameters: "--settings"; WorkingDir: "{app}"; Comment: "{cm:AppComment}"; Tasks: desktopicon
Name: "{userprograms}\{cm:UninstallProgram,Cursor Comet}"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CursorComet"; ValueData: """{app}\cursor-comet.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\cursor-comet.exe"; Description: "{cm:LaunchProgram,Cursor Comet}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\uninstall.exe"

[Code]
var
  ConfigPath: string;
  LegacyConfig: string;

procedure KillRunningApp;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM cursor-comet.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure RemoveLegacyInstall;
var
  LegacyKey: string;
begin
  LegacyKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\CursorComet';
  if RegKeyExists(HKCU, LegacyKey) then
    RegDeleteKeyIncludingSubkeys(HKCU, LegacyKey);
  DeleteFile(ExpandConstant('{app}\uninstall.exe'));
end;

procedure WriteLanguageToConfig;
var
  Lines: TArrayOfString;
  I: Integer;
  Found: Boolean;
  LangCode: string;
begin
  ConfigPath := ExpandConstant('{userappdata}\CursorComet\config.ini');
  LegacyConfig := ExpandConstant('{userappdata}\MouseTrail\config.ini');
  ForceDirectories(ExtractFileDir(ConfigPath));
  if (not FileExists(ConfigPath)) and FileExists(LegacyConfig) then
    CopyFile(LegacyConfig, ConfigPath, False);
  LangCode := ActiveLanguage;
  Found := False;
  if LoadStringsFromFile(ConfigPath, Lines) then
  begin
    for I := 0 to GetArrayLength(Lines) - 1 do
    begin
      if Pos('lang=', Lines[I]) = 1 then
      begin
        Lines[I] := 'lang=' + LangCode;
        Found := True;
        break;
      end;
    end;
  end;
  if not Found then
  begin
    SetArrayLength(Lines, GetArrayLength(Lines) + 1);
    Lines[GetArrayLength(Lines) - 1] := 'lang=' + LangCode;
  end;
  SaveStringsToFile(ConfigPath, Lines, False);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  KillRunningApp;
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    RemoveLegacyInstall;
    WriteLanguageToConfig;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RunKey: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RunKey := 'Software\Microsoft\Windows\CurrentVersion\Run';
    if RegValueExists(HKCU, RunKey, 'CursorComet') then
      RegDeleteValue(HKCU, RunKey, 'CursorComet');
    if RegValueExists(HKCU, RunKey, 'MouseTrail') then
      RegDeleteValue(HKCU, RunKey, 'MouseTrail');
  end;
end;
