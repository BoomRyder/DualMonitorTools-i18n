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
english.ErrDmtRunning=Dual Monitor Tools is currently running and has to be closed before installing.%n%nClose it now? Unsaved magic word edits will be lost.
chinesesimplified.ErrDmtRunning=Dual Monitor Tools 正在运行，安装前需要先关闭它。%n%n现在就关闭吗？未保存的魔术字修改会丢失。
english.ErrDmtNotClosed=Dual Monitor Tools is still running.%n%nPlease exit it from the system tray, then run this installer again.
chinesesimplified.ErrDmtNotClosed=Dual Monitor Tools 仍在运行。%n%n请从系统托盘退出它，然后重新运行本安装程序。
english.ErrNeedAdmin=Setup is not running with administrator privileges and cannot write to "%1".%n%nRight-click the setup program, choose "Run as administrator", and try again.
chinesesimplified.ErrNeedAdmin=安装程序没有获得管理员权限，无法写入「%1」。%n%n请右键点击安装程序，选择「以管理员身份运行」后重试。
english.ErrCannotWrite=Setup could not create the directory%n%n%1%n%nIf you are already running as administrator, antivirus or "folder protection" software is most likely blocking writes to this location. Allow the setup in your antivirus, or run it with /LOG="C:\path\setup.log" and check that log.
chinesesimplified.ErrCannotWrite=无法创建目录%n%n%1%n%n若已以管理员身份运行仍然失败，通常是杀毒软件或「文件夹保护」拦截了写入。请在杀毒软件中放行本安装程序，或用 /LOG="C:\路径\setup.log" 运行并查看日志。

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
; 桌面快捷方式。admin 模式下 {autodesktop} 即公共桌面，卸载时自动删除
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\DMT.exe"; WorkingDir: "{app}"; IconFilename: "{app}\DMT.exe"

[Registry]
; 与原 MSI 一致：仅标记值，源码中无任何读写
Root: HKLM; Subkey: "Software\GNE\Dual Monitor Tools"; ValueType: dword; ValueName: "ScreenSaverInstalled"; ValueData: "1"; Flags: uninsdeletekey
; 这里不要建 HKCU 的右键菜单空键。原 MSI 建了个空占位键，Explorer 会把裸键名
; GNE_DualMonitorTools 当成一个菜单项显示，点下去报"没有与之关联的应用"。真正的
; 菜单树由 DMT 自己按用户的勾选创建（DesktopContextMenu.Add），卸载时由 [Code] 清掉。

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
	ScreenSaverName = 'DmtWallpaper.scr';
	SPI_SETDESKWALLPAPER = 20;
	SPIF_UPDATEINIFILE = 1;
	SPIF_SENDCHANGE = 2;

function DotNetIsPresent(): Boolean;
var
	Install: Cardinal;
begin
	Result :=
		(RegQueryDWordValue(HKLM, DotNetClientKey, 'Install', Install) and (Install = 1)) or
		(RegQueryDWordValue(HKLM64, DotNetClientKey, 'Install', Install) and (Install = 1));
end;

function LegacyMsiIsPresent(): Boolean;
var
	Installed: Cardinal;
begin
	// Only the "installed" *value* marks the original MSI. Do not test for the
	// key alone: this installer writes ScreenSaverInstalled into the same key,
	// so a key-existence test would refuse to run a second time.
	Result :=
		RegKeyExists(HKLM, LegacyMsiProductCode) or
		RegKeyExists(HKLM64, LegacyMsiProductCode) or
		(RegQueryDWordValue(HKLM, LegacyInstalledKey, 'installed', Installed) and (Installed = 1)) or
		(RegQueryDWordValue(HKLM64, LegacyInstalledKey, 'installed', Installed) and (Installed = 1));
end;

function DirectoryIsWritable(const Dir: String): Boolean;
var
	Probe: String;
begin
	// Creating the directory is not enough: antivirus "folder protection" lets
	// the mkdir through and blocks the file writes that follow.
	Result := False;
	if not ForceDirectories(Dir) then
		Exit;

	Probe := AddBackslash(Dir) + '.dmt-write-test.tmp';
	if not SaveStringToFile(Probe, 'probe', False) then
		Exit;

	DeleteFile(Probe);
	Result := True;
end;

function WaitForDmtToExit(TimeoutMs: Integer): Boolean;
var
	Waited: Integer;
begin
	Waited := 0;
	while (FindWindowByWindowName(DmtWindowName) <> 0) and (Waited < TimeoutMs) do
	begin
		Sleep(200);
		Waited := Waited + 200;
	end;
	Result := FindWindowByWindowName(DmtWindowName) = 0;
end;

function StopRunningDmt(): Boolean;
var
	ResultCode: Integer;
begin
	// Graceful first: DMT saves its magic words when it gets WM_CLOSE, so a
	// plain taskkill keeps the edits made since the last save. Force only when
	// it refuses to go away on its own.
	Exec(ExpandConstant('{sysnative}\taskkill.exe'), '/IM DMT.exe', '',
		SW_HIDE, ewWaitUntilTerminated, ResultCode);
	if WaitForDmtToExit(5000) then
	begin
		Log('closed the running DMT gracefully');
		Result := True;
		Exit;
	end;

	Log('DMT ignored WM_CLOSE, killing it');
	Exec(ExpandConstant('{sysnative}\taskkill.exe'), '/F /IM DMT.exe', '',
		SW_HIDE, ewWaitUntilTerminated, ResultCode);
	Result := WaitForDmtToExit(5000);
	if Result then
		Log('killed the running DMT')
	else
		Log('DMT is still running after taskkill /F');
end;

function InitializeSetup(): Boolean;
var
	AppDir: String;
begin
	Result := False;

	// PrivilegesRequired=admin makes SetupLdr relaunch itself elevated, so this
	// is normally already true. It is still checked: a Run entry, a scheduled
	// task or /CURRENTUSER can start Setup unelevated, and without this gate the
	// first symptom would be a bare "unable to create the directory" message.
	if not IsAdmin then
	begin
		AppDir := ExpandConstant('{autopf}\{#MyAppName}');
		SuppressibleMsgBox(FmtMessage(CustomMessage('ErrNeedAdmin'), [AppDir]),
			mbCriticalError, MB_OK, IDOK);
		Exit;
	end;

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
		if SuppressibleMsgBox(CustomMessage('ErrDmtRunning'), mbConfirmation,
			MB_YESNO, IDYES) <> IDYES then
			Exit;

		if not StopRunningDmt() then
		begin
			SuppressibleMsgBox(CustomMessage('ErrDmtNotClosed'), mbCriticalError, MB_OK, IDOK);
			Exit;
		end;
	end;

	Result := True;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
	AppDir: String;
	Existed: Boolean;
begin
	// Replace Inno's generic "unable to create the directory" failure during the
	// file copy with a message that names the path and the likely cause.
	Result := '';
	AppDir := ExpandConstant('{app}');
	Existed := DirExists(AppDir);

	if not DirectoryIsWritable(AppDir) then
	begin
		Result := FmtMessage(CustomMessage('ErrCannotWrite'), [AppDir]);
		Exit;
	end;

	// The probe may have created {app}. Drop it again so Setup creates and
	// records it itself: a directory Setup did not create is not removed on
	// uninstall, which leaves an empty folder behind.
	if not Existed then
		RemoveDir(AppDir);
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
		if not RegWriteStringValue(HKCU, DesktopKey, 'Wallpaper', '') then
			Log('FAILED: could not clear the Wallpaper value');

		// SPIF_SENDCHANGE repaints the desktop now instead of at next logon;
		// without it the deleted wallpaper stays on screen.
		if SystemParametersInfoW(SPI_SETDESKWALLPAPER, 0, '',
			SPIF_UPDATEINIFILE or SPIF_SENDCHANGE) then
			Log('cleared DMT-managed wallpaper: ' + Wallpaper)
		else
			Log('WARNING: SystemParametersInfo failed; registry value cleared anyway');
	except
		Log('ClearWallpaperIfOurs failed');
	end;
end;

procedure RemoveGhostContextMenuKey();
begin
	// A shell verb key with no command subkey is not a working verb: Explorer still
	// lists it under its raw key name, and clicking it fails with "no application is
	// associated with the specified file". The original MSI created exactly such an
	// empty placeholder, and so did earlier builds of this installer.
	if not RegKeyExists(HKCU, DmtContextMenuKey) then
		Exit;

	// The real menu always has a \shell subtree (DesktopContextMenu.Add); without it
	// there is no command to invoke and the key is pure leftover.
	if RegKeyExists(HKCU, DmtContextMenuKey + '\shell') then
		Exit;

	RegDeleteKeyIncludingSubkeys(HKCU, DmtContextMenuKey);
	Log('removed the empty context menu key left by an older install');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if CurStep = ssPostInstall then
		RemoveGhostContextMenuKey();
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
	DataDir: String;
	ScreenSaver: String;
	Suffix: String;
begin
	if CurUninstallStep <> usPostUninstall then
		Exit;

	// The functions below return Boolean and never raise, so their results are
	// checked individually: a swallowed failure must not be logged as success.
	// 1-2. Wallpaper: only when it points into our data directory.
	ClearWallpaperIfOurs();

	// 3. Interface language and wallpaper path.
	if RegDeleteKeyIncludingSubkeys(HKCU, DmtProductKey) then
		Log('deleted HKCU\' + DmtProductKey)
	else
		Log('FAILED: could not delete HKCU\' + DmtProductKey);

	// 3b. [Registry] removes our key but leaves the empty vendor key behind.
	if RegDeleteKeyIfEmpty(HKLM, LegacyInstalledKey) then
		Log('removed ' + LegacyInstalledKey)
	else
		Log('note: ' + LegacyInstalledKey + ' absent or not empty');
	if RegDeleteKeyIfEmpty(HKLM, LegacyVendorKey) then
		Log('removed ' + LegacyVendorKey)
	else
		Log('note: ' + LegacyVendorKey + ' absent or not empty');
	RegDeleteKeyIfEmpty(HKLM64, LegacyInstalledKey);
	RegDeleteKeyIfEmpty(HKLM64, LegacyVendorKey);

	// 4. Auto-start value.
	if RegDeleteValue(HKCU, AutoStartRunKey, AutoStartValueName) then
		Log('deleted Run value ' + AutoStartValueName)
	else
		Log('note: Run value ' + AutoStartValueName + ' absent');

	// 5. Explorer context menu subtree, including the sub-commands the program
	//    creates at run time. Deleting the empty key alone would leave them.
	if RegDeleteKeyIncludingSubkeys(HKCU, DmtContextMenuKey) then
		Log('deleted ' + DmtContextMenuKey)
	else
		Log('FAILED: could not delete ' + DmtContextMenuKey);

	// 6. A screen-saver selection pointing at the .scr we just removed. The
	//    value is written by the Control Panel, not by us, but deleting the
	//    file would leave it dangling.
	Suffix := '\' + ScreenSaverName;
	if RegQueryStringValue(HKCU, DesktopKey, 'SCRNSAVE.EXE', ScreenSaver) and
		(Length(ScreenSaver) >= Length(Suffix)) and
		(CompareText(Copy(ScreenSaver, Length(ScreenSaver) - Length(Suffix) + 1,
			Length(Suffix)), Suffix) = 0) then
	begin
		if RegDeleteValue(HKCU, DesktopKey, 'SCRNSAVE.EXE') then
			Log('deleted SCRNSAVE.EXE pointing at ' + ScreenSaver)
		else
			Log('FAILED: could not delete SCRNSAVE.EXE');
	end;

	// 7. Program data directory. Deleted unconditionally; see README-installer.
	DataDir := ExpandConstant('{userappdata}\' + DataFolderName);
	if DelTree(DataDir, True, True, True) then
		Log('deleted ' + DataDir)
	else
		Log('note: ' + DataDir + ' absent or partially removed');

	// 8. Leftover update download (%TEMP%\DualMonitorTools.msi).
	if DeleteFile(GetEnv('TEMP') + '\DualMonitorTools.msi') then
		Log('deleted leftover update package')
	else
		Log('note: no leftover update package');
end;
