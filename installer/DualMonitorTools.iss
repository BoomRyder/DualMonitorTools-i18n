; Dual Monitor Tools 2.13 (简体中文) —— Inno Setup 安装脚本
; 设计说明：docs/superpowers/specs/2026-11-02-dmt-inno-installer-design.md

#define MyAppName "Dual Monitor Tools"
#define MyAppVersion "2.13"
#define MyAppPublisher "GNE"
#define MyAppURL "http://dualmonitortool.sourceforge.net/"

; 仓库根。build-installer.ps1 通过 /DRepoRoot=... 传入；
; 直接用 ISIDE 打开时回退到脚本所在目录的上一级。
#ifndef RepoRoot
  #define RepoRoot SourcePath + ".."
#endif

[Setup]
AppId={{91A49D81-5FF8-421E-A669-94A906D5D700}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion} (简体中文)
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=no
LicenseFile={#RepoRoot}\COPYING.txt
OutputBaseFilename=DualMonitorTools-{#MyAppVersion}-zh-Hans-Setup
SetupIconFile={#RepoRoot}\DMT\DMT\DMT.ico
UninstallDisplayIcon={app}\DMT.exe
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Files]
; 主程序与配置文件
Source: "{#RepoRoot}\DMT\DMT\bin\Release\DMT.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\DMT\DMT\bin\Release\DMT.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe.config"; DestDir: "{app}"; Flags: ignoreversion

; 中文卫星程序集 —— 必须落在 {app}\zh-Hans\ 下，放错目录中文界面就失效
Source: "{#RepoRoot}\DMT\DMT\bin\Release\zh-Hans\DMT.resources.dll"; DestDir: "{app}\zh-Hans"; Flags: ignoreversion
Source: "{#RepoRoot}\DualWallpaper\DualWallpaper\bin\Release\zh-Hans\DualWallpaper.resources.dll"; DestDir: "{app}\zh-Hans"; Flags: ignoreversion
Source: "{#RepoRoot}\DmtWallpaper\DmtWallpaper\bin\Release\zh-Hans\DmtWallpaper.resources.dll"; DestDir: "{app}\zh-Hans"; Flags: ignoreversion

; 数据目录重定向
Source: "{#RepoRoot}\installer\DmtFileLocations.xml"; DestDir: "{app}"; Flags: ignoreversion

; 文档
Source: "{#RepoRoot}\COPYING.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion

; 屏保 —— 装到 System32，64 位 desk.cpl 才能枚举到（原 MSI 装 SysWOW64 是缺陷）
Source: "{#RepoRoot}\DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.scr"; DestDir: "{sys}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\DMT.exe"
Name: "{group}\DualWallpaper"; Filename: "{app}\DualWallpaper.exe"

[Registry]
; 与原 MSI 一致：仅标记值，源码中无任何读写
Root: HKLM; Subkey: "Software\GNE\Dual Monitor Tools"; ValueType: dword; ValueName: "ScreenSaverInstalled"; ValueData: "1"; Flags: uninsdeletevalue
; 与原 MSI 一致：右键菜单空键。注意运行期会生成子命令，卸载由 [Code] 兜底
Root: HKCU; Subkey: "Software\Classes\directory\Background\shell\GNE_DualMonitorTools"; Flags: uninsdeletekey

[Run]
Filename: "{app}\DMT.exe"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent runasoriginaluser
