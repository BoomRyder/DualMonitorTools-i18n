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

[CustomMessages]
english.ErrNoDotNet=This program requires Microsoft .NET Framework 4.0 or later.%n%nPlease install .NET Framework 4.8 and then run the installer again.
chinesesimplified.ErrNoDotNet=本程序需要 Microsoft .NET Framework 4.0 或更高版本。%n%n请先安装 .NET Framework 4.8 后重新运行安装程序。
english.ErrLegacyMsi=An existing Dual Monitor Tools installation (the original MSI package) was found.%n%nPlease uninstall it from "Programs and Features" first, then run this installer again.
chinesesimplified.ErrLegacyMsi=检测到已安装的 Dual Monitor Tools 原版（MSI）。%n%n请先在「程序和功能」中将其卸载，然后再运行本安装程序。
english.ErrDmtRunning=Dual Monitor Tools is currently running.%n%nPlease right-click the DMT icon in the system tray, choose Exit, and then run this installer again.
chinesesimplified.ErrDmtRunning=Dual Monitor Tools 正在运行。%n%n请先右键点击系统托盘中的 DMT 图标并退出，然后重新运行安装程序。

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
Source: "{#RepoRoot}\DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.scr"; DestDir: "{sysnative}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\DMT.exe"
Name: "{group}\DualWallpaper"; Filename: "{app}\DualWallpaper.exe"

[Registry]
; 与原 MSI 一致：仅标记值，源码中无任何读写
Root: HKLM; Subkey: "Software\GNE\Dual Monitor Tools"; ValueType: dword; ValueName: "ScreenSaverInstalled"; ValueData: "1"; Flags: uninsdeletekey
; 与原 MSI 一致：右键菜单空键。注意运行期会生成子命令，卸载由 [Code] 兜底
Root: HKCU; Subkey: "Software\Classes\directory\Background\shell\GNE_DualMonitorTools"; Flags: uninsdeletekey

[Run]
Filename: "{app}\DMT.exe"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
const
	LegacyMsiProductCode =
		'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{59CFB0C1-3F5B-49B6-B1C9-C473153C1257}';
	LegacyInstalledKey = 'SOFTWARE\GNE\Dual Monitor Tools';
	LegacyVendorKey = 'SOFTWARE\GNE';
	DotNetClientKey = 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client';
	DmtWindowName = 'DMT_GUI_WINDOW';
	DmtProductKey = 'SOFTWARE\Dual Monitor Tools';
	DmtContextMenuKey = 'Software\Classes\directory\Background\shell\GNE_DualMonitorTools';
	AutoStartRunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
	AutoStartValueName = 'GNE_DualMonitorTools';
	DesktopKey = 'Control Panel\Desktop';
	DataFolderName = 'Dual Monitor Tools';
	SPI_SETDESKWALLPAPER = 20;
	SPIF_UPDATEINIFILE = 1;

function DotNetIsPresent(): Boolean;
var
	Install: Cardinal;
begin
	Result :=
		(RegQueryDWordValue(HKLM, DotNetClientKey, 'Install', Install) and (Install = 1)) or
		(RegQueryDWordValue(HKLM64, DotNetClientKey, 'Install', Install) and (Install = 1));
end;

function LegacyMsiIsPresent(): Boolean;
begin
	Result :=
		RegKeyExists(HKLM, LegacyMsiProductCode) or
		RegKeyExists(HKLM64, LegacyMsiProductCode) or
		RegKeyExists(HKLM, LegacyInstalledKey) or
		RegKeyExists(HKLM64, LegacyInstalledKey);
end;

function InitializeSetup(): Boolean;
begin
	Result := False;

	if not DotNetIsPresent() then
	begin
		SuppressibleMsgBox(CustomMessage('ErrNoDotNet'), mbCriticalError, MB_OK, IDOK);
		Exit;
	end;

	if LegacyMsiIsPresent() then
	begin
		SuppressibleMsgBox(CustomMessage('ErrLegacyMsi'), mbCriticalError, MB_OK, IDOK);
		Exit;
	end;

	if FindWindowByWindowName(DmtWindowName) <> 0 then
	begin
		SuppressibleMsgBox(CustomMessage('ErrDmtRunning'), mbCriticalError, MB_OK, IDOK);
		Exit;
	end;

	Result := True;
end;

function SystemParametersInfoW(uAction: Cardinal; uParam: Cardinal; pvParam: String;
	fWinIni: Cardinal): Boolean;
	external 'SystemParametersInfoW@user32.dll stdcall';

procedure ClearWallpaperIfOurs();
var
	Wallpaper: String;
	OurPrefix: String;
begin
	try
		if not RegQueryStringValue(HKCU, DesktopKey, 'Wallpaper', Wallpaper) then
			Exit;

		if Wallpaper = '' then
			Exit;

		OurPrefix := ExpandConstant('{userappdata}\' + DataFolderName + '\');

		if CompareText(Copy(Wallpaper, 1, Length(OurPrefix)), OurPrefix) <> 0 then
			Exit;

		// Option (a): clear to a solid colour instead of pointing at a file a
		// trimmed Windows install may not ship.
		RegWriteStringValue(HKCU, DesktopKey, 'Wallpaper', '');
		SystemParametersInfoW(SPI_SETDESKWALLPAPER, 0, '', SPIF_UPDATEINIFILE);
		Log('cleared DMT-managed wallpaper: ' + Wallpaper);
	except
		Log('ClearWallpaperIfOurs failed');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
	DataDir: String;
begin
	if CurUninstallStep <> usPostUninstall then
		Exit;

	// 1-2. Wallpaper: only when it points into our data directory.
	ClearWallpaperIfOurs();

	// 3. Interface language and wallpaper path.
	try
		RegDeleteKeyIncludingSubkeys(HKCU, DmtProductKey);
		Log('deleted HKCU\' + DmtProductKey);
	except
		Log('failed to delete HKCU\' + DmtProductKey);
	end;

	// 3b. [Registry] removes our key but leaves the empty vendor key behind.
	try
		RegDeleteKeyIfEmpty(HKLM, LegacyInstalledKey);
		RegDeleteKeyIfEmpty(HKLM, LegacyVendorKey);
		Log('removed empty vendor key');
	except
		Log('failed to remove empty vendor key');
	end;

	// 4. Auto-start value.
	try
		RegDeleteValue(HKCU, AutoStartRunKey, AutoStartValueName);
		Log('deleted Run value ' + AutoStartValueName);
	except
		Log('failed to delete Run value');
	end;

	// 5. Explorer context menu subtree, including the sub-commands the program
	//    creates at run time. Deleting the empty key alone would leave them.
	try
		RegDeleteKeyIncludingSubkeys(HKCU, DmtContextMenuKey);
		Log('deleted ' + DmtContextMenuKey);
	except
		Log('failed to delete context menu subtree');
	end;

	// 6. Program data directory.
	DataDir := ExpandConstant('{userappdata}\' + DataFolderName);
	try
		DelTree(DataDir, True, True, True);
		Log('deleted ' + DataDir);
	except
		Log('failed to delete ' + DataDir);
	end;

	// 7. Leftover update download.
	try
		DeleteFile(GetEnv('TEMP') + '\DualMonitorTools.msi');
	except
		Log('failed to delete temp msi');
	end;
end;
