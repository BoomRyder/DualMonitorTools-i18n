# DMT 安装/卸载程序（Inno Setup）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 Dual Monitor Tools 2.13 简体中文版产出 Inno Setup 安装包，完整复刻原 MSI 的安装行为，并在卸载时清干净程序运行时产生的一切注册表与文件。

**Architecture:** 一个 Inno Setup 脚本 `installer\DualMonitorTools.iss`（`[Setup]`/`[Files]`/`[Icons]`/`[Registry]`/`[Run]` 声明式安装 + `[Code]` 段 Pascal 处理前置检查与卸载清理），配套一个数据目录重定向文件 `installer\DmtFileLocations.xml`，由 `build-installer.ps1` 一键构建。静态断言进 NUnit（无副作用、随 `build.ps1 -Test` 跑），需要管理员权限的行为验证放独立脚本 `installer\tests\*.ps1`。

**Tech Stack:** Inno Setup 7.1.0（`D:\Program Files\Inno Setup 7\ISCC.exe`）、ISPP 预处理、PowerShell 5.1、NUnit 3.7.1、.NET Framework 4.5（测试工程）

**Spec:** `docs/superpowers/specs/2026-11-02-dmt-inno-installer-design.md`

## Global Constraints

- 不改原作者源码（硬约束 2）。本计划只新增文件，唯一例外是 `AGENTS.md`（本地文件，不入库）。
- 不升级目标框架；不启动 GUI 做验证（硬约束 3、4）。
- 不写 `HKLM\...\GNE\Dual Monitor Tools\installed` 值 —— 这是关掉自动更新的关键决策，`installed` 一写，`CheckUpdatesForm` 就会下载英文原版 MSI 覆盖中文版。
- `.iss` 与 `.xml` 一律 **UTF-8 with BOM**；`.md` 用 UTF-8 无 BOM + LF（`AGENTS.md` 第 6 节）。
- `.cs` 文件：Tab 缩进、CRLF、UTF-8 with BOM、块式 namespace、`using` 写在 namespace 内、文件头 GPLv3 copyright region。
- 验证门槛：改任何 `.cs` 后必须 `.\build.ps1 -Configuration Release -Test` 通过且 `Failed: 0`。
- 仓库根 `D:\workapsce\common\DMT`，分支 `main`。每次提交前 `git status --porcelain` 必须只剩预期文件。
- 行为测试会真实写 `HKLM`、`HKCU`、`%APPDATA%`、`C:\Windows\System32`，**必须**先保存原始状态、测试后完整复原。`AGENTS.md` 5.9 记录的本机基线是"这些项都不存在"。

## Review Focus

以下是 spec 隐含、但任何单个任务的测试都不会覆盖、且最容易坑到真实用户的失败模式。每条后面注明由哪个任务钉住。

1. **卸载会毁掉用户自己的壁纸** —— 若用户从未用过 DMT 的壁纸变换器，`HKCU\Control Panel\Desktop\Wallpaper` 指向的是他自己设的图。卸载时**必须**先判断该路径是否位于 `%APPDATA%\Dual Monitor Tools\` 之下，否则清空它等于白改用户桌面。→ Task 4
2. **右键菜单子树残留** —— 用户只要点过一次"DMT 右键菜单"，就会生成 11 条子命令。只删空键（`uninsdeletekey`）会全部留下，用户右键还会看到一堆指向已删除 exe 的死项。→ Task 4
3. **装完就自动更新成英文原版** —— 测一下 `installed` 值是否存在，存在就必须判为失败。这是本设计唯一"看起来更完整、实际是陷阱"的地方。→ Task 2
4. **静默安装时前置检查弹窗卡死** —— `InitializeSetup` 里若用普通 `MsgBox`，`/VERYSILENT` 场景（CI、企业部署）会挂住等不到人点。必须 `SuppressibleMsgBox` 或在 `Silent` 时跳过弹窗。→ Task 3
5. **中文界面失效** —— 三个 `zh-Hans\*.resources.dll` 若漏装或放错目录（必须放进 `{app}\zh-Hans\` 子目录而非 `{app}` 根），中文版就退化成英文。→ Task 2 钉"有没有装上"，Task 4 钉"落在哪个子目录"

---

### Task 1: 数据目录重定向文件

**Files:**
- Create: `installer\DmtFileLocations.xml`
- Test: `DMT\DMT.UnitTests\Installer\InstallerAssets_Test.cs`

**Interfaces:**
- Consumes: 无
- Produces: `installer\DmtFileLocations.xml`，供 Task 2 的 `[Files]` 段安装到 `{app}`。文件内容必须是单个 `<filelocation name="datadirectory" value="%APPDATA%\Dual Monitor Tools" />` 元素，因为 `FileLocations.GetLocationRemaps` 只认 `filelocation` 元素的 `name`/`value` 属性。

- [ ] **Step 1: 写失败的测试**

创建 `DMT\DMT.UnitTests\Installer\InstallerAssets_Test.cs`。沿用 `DMT\DMT.UnitTests\Library\Localization\ResourceCoverage_Test.cs:106-114` 的 `RepoRoot` 写法（`BaseDirectory` 上溯 4 级）。文件头照抄现有测试文件的 GPLv3 copyright region。

```csharp
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace DMT.UnitTests.Installer
{
	public class InstallerAssets_Test
	{
		static string RepoRoot
		{
			get
			{
				return Path.GetFullPath(
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
			}
		}

		static string FileLocationsXml
		{
			get { return Path.Combine(RepoRoot, @"installer\DmtFileLocations.xml"); }
		}

		[Test]
		public void FileLocationsXmlExists()
		{
			Assert.IsTrue(File.Exists(FileLocationsXml),
				"missing: installer\\DmtFileLocations.xml");
		}

		[Test]
		public void FileLocationsXmlIsUtf8WithBom()
		{
			byte[] head = new byte[3];
			using (FileStream s = File.OpenRead(FileLocationsXml))
			{
				Assert.AreEqual(3, s.Read(head, 0, 3), "file too short");
			}

			Assert.AreEqual(0xEF, head[0], "missing UTF-8 BOM");
			Assert.AreEqual(0xBB, head[1], "missing UTF-8 BOM");
			Assert.AreEqual(0xBF, head[2], "missing UTF-8 BOM");
		}

		[Test]
		public void FileLocationsXmlIsWellFormed()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(FileLocationsXml);   // throws on malformed XML
			Assert.IsNotNull(doc.DocumentElement);
		}

		[Test]
		public void FileLocationsXmlRedirectsDataDirectoryToAppData()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(FileLocationsXml);

			XmlNodeList nodes = doc.SelectNodes("/filelocations/filelocation");
			Assert.AreEqual(1, nodes.Count,
				"expected exactly one active filelocation entry");

			Assert.AreEqual("datadirectory", nodes[0].Attributes["name"].Value);
			Assert.AreEqual(@"%APPDATA%\Dual Monitor Tools",
				nodes[0].Attributes["value"].Value);
		}
	}
}
```

- [ ] **Step 2: 跑测试确认失败**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: 4 个新测试全 FAIL，报 `missing: installer\DmtFileLocations.xml`（其余原有测试仍应 0 failed）

- [ ] **Step 3: 创建 `installer\DmtFileLocations.xml`（UTF-8 **with BOM**，CRLF）**

```xml
<?xml version="1.0" encoding="utf-8"?>
<!--
    Dual Monitor Tools 数据文件位置。
    本文件必须与 DMT.exe 位于同一目录，且程序不会改写它，
    因此放在 C:\Program Files (x86) 下是安全的。
-->
<filelocations>
    <filelocation name="datadirectory" value="%APPDATA%\Dual Monitor Tools" />
</filelocations>
```

用 PowerShell 写入，注意 `UTF8Encoding($true)` 才带 BOM：

```powershell
$path = 'D:\workapsce\common\DMT\installer\DmtFileLocations.xml'
$text = @'
<?xml version="1.0" encoding="utf-8"?>
<!--
    Dual Monitor Tools 数据文件位置。
    本文件必须与 DMT.exe 位于同一目录，且程序不会改写它，
    因此放在 C:\Program Files (x86) 下是安全的。
-->
<filelocations>
    <filelocation name="datadirectory" value="%APPDATA%\Dual Monitor Tools" />
</filelocations>
'@
$text = $text -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($true)))
```

- [ ] **Step 4: 跑测试确认通过**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: 全部通过，`Failed: 0`

- [ ] **Step 5: 提交**

```bash
git add installer/DmtFileLocations.xml DMT/DMT.UnitTests/Installer/InstallerAssets_Test.cs
git commit -m "installer: add data-directory redirect file with asset guards"
```

---

### Task 2: `.iss` 骨架与构建脚本

**Files:**
- Create: `installer\DualMonitorTools.iss`
- Create: `build-installer.ps1`
- Modify: `DMT\DMT.UnitTests\Installer\InstallerAssets_Test.cs`（追加 3 个测试）
- Modify: `.gitignore`（加 `dist/`）

**Interfaces:**
- Consumes: `installer\DmtFileLocations.xml`（Task 1）
- Produces: `dist\DualMonitorTools-2.13-zh-Hans-Setup.exe`；`.iss` 中的常量 `MyAppName`/`MyAppVersion`/`MyAppPublisher`/`MyAppURL`/`RepoRoot`，Task 3、Task 4 在同一脚本里追加 `[Code]` 段。

**这一步不加 `[Code]` 段**，先让包能编出来，用静态断言钉住关键决策。

- [ ] **Step 1: 写失败的测试**

在 `InstallerAssets_Test.cs` 里追加（`using System.Text;` 与 `using System.Collections.Generic;` 需要补上）：

```csharp
static string IssPath
{
	get { return Path.Combine(RepoRoot, @"installer\DualMonitorTools.iss"); }
}

static string ReadIss()
{
	return File.ReadAllText(IssPath, Encoding.UTF8);
}

[Test]
public void IssExists()
{
	Assert.IsTrue(File.Exists(IssPath), "missing: installer\\DualMonitorTools.iss");
}

[TestCase(@"DefaultDirName={autopf}\Dual Monitor Tools")]
[TestCase(@"DefaultGroupName=Dual Monitor Tools")]
[TestCase("PrivilegesRequired=admin")]
[TestCase("ArchitecturesAllowed=x64compatible")]
[TestCase("DisableProgramGroupPage=yes")]
[TestCase("CloseApplications=yes")]
public void IssDeclaresRequiredSetupDirective(string expectedLine)
{
	string iss = ReadIss();

	Assert.IsTrue(iss.IndexOf(expectedLine, System.StringComparison.Ordinal) >= 0,
		"missing [Setup] directive: " + expectedLine);
}

[Test]
public void IssDoesNotWriteTheInstalledMarker()
{
	string iss = ReadIss();

	// 写这个值会让 CheckUpdatesForm 显示"安装新版本"，
	// 而该按钮会下载并运行英文原版 MSI，覆盖中文版。
	Assert.IsFalse(iss.IndexOf("ValueName: \"installed\"", System.StringComparison.Ordinal) >= 0,
		"the 'installed' marker must never be written");
}

[TestCase(@"DMT\DMT\bin\Release\DMT.exe")]
[TestCase(@"DMT\DMT\bin\Release\DMT.exe.config")]
[TestCase(@"DMT\DMT\bin\Release\zh-Hans\DMT.resources.dll")]
[TestCase(@"DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe")]
[TestCase(@"DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe.config")]
[TestCase(@"DualWallpaper\DualWallpaper\bin\Release\zh-Hans\DualWallpaper.resources.dll")]
[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe")]
[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe.config")]
[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\zh-Hans\DmtWallpaper.resources.dll")]
[TestCase(@"DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.scr")]
public void IssShipsEveryReleaseArtifact(string relativePath)
{
	string iss = ReadIss();

	string fileName = Path.GetFileName(relativePath);

	Assert.IsTrue(iss.IndexOf(fileName, System.StringComparison.OrdinalIgnoreCase) >= 0,
		"not shipped by the installer: " + relativePath);

	Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, relativePath)),
		"missing build output (run build.ps1 -Configuration Release first): " + relativePath);
}
```

- [ ] **Step 2: 跑测试确认失败**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `IssExists` 与所有 `TestCase` 变体 FAIL（`missing: installer\DualMonitorTools.iss`），`IssDoesNotWriteTheInstalledMarker` 也会 FAIL（文件不存在时 `ReadIss()` 抛 `FileNotFoundException`）

- [ ] **Step 3: 创建 `installer\DualMonitorTools.iss`（UTF-8 **with BOM**）**

全部取值由 spec §3–§5 固定，逐字照抄：

```ini
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
```

- [ ] **Step 4: 创建 `build-installer.ps1`（UTF-8 **with BOM**，因为含中文）**

```powershell
<#
    Dual Monitor Tools 2.13 —— 一键构建安装包
    用法：
        .\build-installer.ps1
        .\build-installer.ps1 -SkipBuild     # 复用已有 bin\Release
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot

function Find-InnoCompiler {
    # 1) 注册表（Inno 7 把自身卸载项写在 64 位视图）
    $regPaths = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1'
    )
    foreach ($rp in $regPaths) {
        if (Test-Path $rp) {
            $loc = (Get-ItemProperty $rp -ErrorAction SilentlyContinue).InstallLocation
            if ($loc) {
                $exe = Join-Path $loc 'ISCC.exe'
                if (Test-Path $exe) { return $exe }
            }
        }
    }

    # 2) 环境变量
    if ($env:INNO_SETUP_DIR) {
        $exe = Join-Path $env:INNO_SETUP_DIR 'ISCC.exe'
        if (Test-Path $exe) { return $exe }
    }

    # 3) 常见安装目录
    foreach ($d in @('D:\Program Files\Inno Setup 7', 'C:\Program Files (x86)\Inno Setup 7', 'C:\Program Files\Inno Setup 7')) {
        $exe = Join-Path $d 'ISCC.exe'
        if (Test-Path $exe) { return $exe }
    }

    throw "未找到 ISCC.exe。请安装 Inno Setup 7，或在安装后设置环境变量 INNO_SETUP_DIR 指向其安装目录。"
}

$iscc = Find-InnoCompiler
Write-Host "ISCC    : $iscc" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Write-Host "`n=== 构建 Release 产物 ===" -ForegroundColor Cyan
    # build.ps1 失败时自己 throw，异常会直接向上冒泡（本脚本 $ErrorActionPreference=Stop），
    # 不需要额外检查退出码 —— .ps1 不会设置 $LASTEXITCODE。
    & (Join-Path $Root 'build.ps1') -Configuration Release
}

$dist = Join-Path $Root 'dist'
if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }

$iss = Join-Path $Root 'installer\DualMonitorTools.iss'
Write-Host "`n=== 编译安装包 ===" -ForegroundColor Cyan
& $iscc "/DRepoRoot=$Root" "/O$dist" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC 编译失败（$LASTEXITCODE）。" }

Write-Host "`n=== 产物 ===" -ForegroundColor Cyan
Get-ChildItem $dist -Filter '*.exe' | ForEach-Object {
    "{0,10}  {1}" -f $_.Length, $_.Name
}
```

- [ ] **Step 5: 在 `.gitignore` 追加 `dist/`**

在文件末尾加一行（保持既有风格，LF 或 CRLF 与原文一致）：

```
dist/
```

- [ ] **Step 6: 编译，确认 `.iss` 语法正确**

Run: `.\build-installer.ps1`
Expected: 结尾含 `Successful compile`，`dist\DualMonitorTools-2.13-zh-Hans-Setup.exe` 存在

若报 `Source file ... does not exist`，说明 `#define RepoRoot SourcePath + ".."` 的路径拼接有误；`.ps1` 已显式传 `/DRepoRoot`，直接用 `.\build-installer.ps1` 跑不会走回退分支。要验证回退分支本身，可 `cd installer; & "D:\Program Files\Inno Setup 7\ISCC.exe" DualMonitorTools.iss /O..\dist`。

- [ ] **Step 7: 跑静态断言测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: 全部通过，`Failed: 0`

- [ ] **Step 8: 提交**

```bash
git add installer/DualMonitorTools.iss build-installer.ps1 .gitignore DMT/DMT.UnitTests/Installer/InstallerAssets_Test.cs
git commit -m "installer: add Inno Setup script and build script"
```

---

### Task 3: 前置检查（`[Code]` 段）

**Files:**
- Modify: `installer\DualMonitorTools.iss`（追加 `[Code]` 段）
- Create: `installer\tests\Test-Preflight.ps1`

**Interfaces:**
- Consumes: Task 2 的 `.iss`；`find-window` 用到的 `DMT_GUI_WINDOW` 常量（`DMT\DMT\AppForm.Designer.cs`）
- Produces: `[Code]` 段的 `DotNetIsPresent(): Boolean`、`LegacyMsiIsPresent(): Boolean`、`InitializeSetup(): Boolean`；`installer\tests\Test-Preflight.ps1` 供 Task 4 的测试脚本复用

- [ ] **Step 1: 先写行为测试 `installer\tests\Test-Preflight.ps1`（UTF-8 with BOM）**

测试方式：伪造"原版 MSI 已安装"的注册表标记，然后用 `/VERYSILENT /SUPPRESSMSGBOXES` 跑安装包，断言它**拒绝安装**且没留下文件。

```powershell
<#
    验证前置检查：伪造"原版 MSI 已安装"标记后，安装包必须拒绝安装。
    需要管理员权限。测试后完整复原注册表。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SetupExe
)

$ErrorActionPreference = 'Stop'

$legacyKey = 'HKLM:\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools'
$targetDir = Join-Path $env:TEMP ('dmt_preflight_' + [guid]::NewGuid().ToString('N'))

function Assert-True($cond, $msg) {
    if (-not $cond) { throw "断言失败：$msg" }
}

try {
    # --- 前提：基线应不存在 ---
    if (Test-Path $legacyKey) {
        throw "基线已被污染：$legacyKey 已存在。请先清理后再跑。"
    }

    # --- 伪造原版 MSI 的 installed 标记 ---
    New-Item -Path $legacyKey -Force | Out-Null
    New-ItemProperty -Path $legacyKey -Name 'installed' -Value 1 -PropertyType DWord -Force | Out-Null
    Write-Host "已伪造原版 MSI 标记：$legacyKey" -ForegroundColor Yellow

    # --- 静默安装，应当被拒绝 ---
    $p = Start-Process -FilePath $SetupExe `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir" `
        -Wait -PassThru

    Write-Host "安装程序退出码：$($p.ExitCode)"
    Assert-True ($p.ExitCode -ne 0) "存在旧 MSI 时安装程序必须拒绝安装（退出码应非 0）"
    Assert-True (-not (Test-Path (Join-Path $targetDir 'DMT.exe'))) "被拒绝时不应安装任何文件"

    Write-Host "PASS：前置检查正确拦截了旧 MSI 共存" -ForegroundColor Green
}
finally {
    # --- 复原 ---
    if (Test-Path $legacyKey) {
        Remove-Item -Path $legacyKey -Recurse -Force
    }
    $parent = 'HKLM:\SOFTWARE\WOW6432Node\GNE'
    if (Test-Path $parent) {
        $children = Get-ChildItem $parent -ErrorAction SilentlyContinue
        if (-not $children) { Remove-Item -Path $parent -Force -ErrorAction SilentlyContinue }
    }
    if (Test-Path $targetDir) { Remove-Item -Path $targetDir -Recurse -Force }
    Write-Host "已复原注册表与临时目录" -ForegroundColor Yellow
}
```

- [ ] **Step 2: 跑测试确认失败**

Run（在**已提权**的会话里）:
```powershell
.\build-installer.ps1
.\installer\tests\Test-Preflight.ps1 -SetupExe .\dist\DualMonitorTools-2.13-zh-Hans-Setup.exe
```
Expected: FAIL —— 断言 `存在旧 MSI 时安装程序必须拒绝安装` 不成立（此时还没有 `[Code]` 段，安装会成功，退出码 0）

- [ ] **Step 3: 在 `.iss` 末尾追加 `[Code]` 段**

三条检查：.NET 4.x、旧 MSI 共存、DMT 正在运行。**弹窗必须用 `SuppressibleMsgBox`**，否则 `/VERYSILENT` 会挂住。

```ini
[Code]
const
	LegacyMsiProductCode =
		'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{59CFB0C1-3F5B-49B6-B1C9-C473153C1257}';
	LegacyInstalledKey = 'SOFTWARE\GNE\Dual Monitor Tools';
	DotNetClientKey = 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client';
	DmtWindowName = 'DMT_GUI_WINDOW';

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
		SuppressibleMsgBox(
			'本程序需要 Microsoft .NET Framework 4.0 或更高版本。' + #13#10 +
			'请先安装 .NET Framework 4.8 后重新运行安装程序。',
			mbCriticalError, MB_OK, IDOK);
		Exit;
	end;

	if LegacyMsiIsPresent() then
	begin
		SuppressibleMsgBox(
			'检测到已安装的 Dual Monitor Tools 原版（MSI）。' + #13#10 +
			'请先在「程序和功能」中将其卸载，然后再运行本安装程序。',
			mbCriticalError, MB_OK, IDOK);
		Exit;
	end;

	if FindWindowByWindowName(DmtWindowName) <> 0 then
	begin
		SuppressibleMsgBox(
			'Dual Monitor Tools 正在运行。' + #13#10 +
			'请先右键点击系统托盘中的 DMT 图标并退出，然后重新运行安装程序。',
			mbCriticalError, MB_OK, IDOK);
		Exit;
	end;

	Result := True;
end;
```

- [ ] **Step 4: 重新编译并跑测试确认通过**

Run:
```powershell
.\build-installer.ps1
.\installer\tests\Test-Preflight.ps1 -SetupExe .\dist\DualMonitorTools-2.13-zh-Hans-Setup.exe
```
Expected: 输出 `安装程序退出码：1` 与 `PASS：前置检查正确拦截了旧 MSI 共存`；结尾 `已复原注册表与临时目录`

再确认复原干净：
```powershell
Test-Path 'HKLM:\SOFTWARE\WOW6432Node\GNE'   # 必须 False
```

- [ ] **Step 5: 提交**

```bash
git add installer/DualMonitorTools.iss installer/tests/Test-Preflight.ps1
git commit -m "installer: refuse to install alongside the original MSI, missing .NET or a running DMT"
```

---

### Task 4: 卸载清理（`[Code]` 段）

**Files:**
- Modify: `installer\DualMonitorTools.iss`（在 `[Code]` 段追加卸载逻辑）
- Create: `installer\tests\Test-Uninstall.ps1`

**Interfaces:**
- Consumes: Task 2 的 `[Files]`/`[Registry]`、Task 3 的 `[Code]` 段
- Produces: `CurUninstallStepChanged(CurUninstallStep: TUninstallStep)`；`installer\tests\Test-Uninstall.ps1`

- [ ] **Step 1: 先写行为测试 `installer\tests\Test-Uninstall.ps1`（UTF-8 with BOM）**

这是本计划最重要的一条测试：装到一个临时目录，**制造全部运行时残留**，卸载后逐项断言消失，最后复原用户的壁纸设置。

```powershell
<#
    验证卸载清理：静默装到临时目录 -> 制造全部运行时残留 -> 静默卸载 -> 断言全部消失。
    需要管理员权限。会临时改动 HKCU\Control Panel\Desktop 的壁纸设置，测试后复原。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SetupExe
)

$ErrorActionPreference = 'Stop'

$targetDir  = Join-Path $env:TEMP ('dmt_uninst_' + [guid]::NewGuid().ToString('N'))
$dataDir    = Join-Path $env:APPDATA 'Dual Monitor Tools'
$dmtKey     = 'HKCU:\SOFTWARE\Dual Monitor Tools'
$ctxKey     = 'HKCU:\Software\Classes\directory\Background\shell\GNE_DualMonitorTools'
$runKey     = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$desktopKey = 'HKCU:\Control Panel\Desktop'
$ssKey      = 'HKLM:\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools'
$scrPath    = Join-Path $env:WINDIR 'System32\DmtWallpaper.scr'

function Assert-True($cond, $msg) {
    if (-not $cond) { throw "断言失败：$msg" }
}

# --- 保存壁纸原始状态 ---
$origWallpaper     = (Get-ItemProperty $desktopKey -Name Wallpaper     -ErrorAction SilentlyContinue).Wallpaper
$origTileWallpaper = (Get-ItemProperty $desktopKey -Name TileWallpaper -ErrorAction SilentlyContinue).TileWallpaper
$origWallpaperStyle= (Get-ItemProperty $desktopKey -Name WallpaperStyle-ErrorAction SilentlyContinue).WallpaperStyle

try {
    # --- 基线检查：这些必须都不存在，否则测试不可信 ---
    foreach ($p in @($dmtKey, $ctxKey, $dataDir, $ssKey, $scrPath)) {
        Assert-True (-not (Test-Path $p)) "基线已被污染，先手工清理：$p"
    }

    # --- 静默安装 ---
    $p = Start-Process -FilePath $SetupExe `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir" `
        -Wait -PassThru
    Assert-True ($p.ExitCode -eq 0) "安装失败，退出码 $($p.ExitCode)"
    Assert-True (Test-Path (Join-Path $targetDir 'DMT.exe')) "安装后 DMT.exe 不存在"
    Assert-True (Test-Path (Join-Path $targetDir 'zh-Hans\DMT.resources.dll')) "中文卫星程序集未安装到 zh-Hans 子目录"
    Assert-True (Test-Path $scrPath) "屏保未安装到 System32"
    Assert-True (Test-Path $ssKey) "ScreenSaverInstalled 标记未写入"
    Write-Host "安装阶段 PASS" -ForegroundColor Green

    # --- 制造全部运行时残留 ---
    # 1) 界面语言 + 壁纸路径
    New-Item -Path "$dmtKey\DMT" -Force | Out-Null
    New-ItemProperty -Path "$dmtKey\DMT" -Name 'Language' -Value 'zh-Hans' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path "$dmtKey\DMT" -Name 'DmtWallpaperPath' -Value "$dataDir\DmtWallpaper.bmp" -PropertyType String -Force | Out-Null

    # 2) 开机自启
    if (-not (Test-Path $runKey)) { New-Item -Path $runKey -Force | Out-Null }
    New-ItemProperty -Path $runKey -Name 'GNE_DualMonitorTools' -Value "$targetDir\DMT.exe" -PropertyType String -Force | Out-Null

    # 3) 右键菜单子树（模拟运行期生成的 11 条子命令）
    foreach ($sub in @('Options', 'MagicWord', 'AddMagicWord', 'MakePrimary', 'ChangeBrightness',
                       'ChangeContrast', 'ChangeOrientation', 'MinimizeAll', 'MoveAll',
                       'ChangeWallpaper')) {
        $k = "$ctxKey\$sub\command"
        New-Item -Path $k -Force | Out-Null
        New-ItemProperty -Path $k -Name '(default)' -Value "`"$targetDir\DMT.exe`" DMT:General:$sub" -PropertyType String -Force | Out-Null
    }

    # 4) 数据目录与其中的文件
    New-Item -Path $dataDir -Force | Out-Null
    Set-Content -Path (Join-Path $dataDir 'DmtSettings.xml') -Value '<settings><setting name="___FirstRun" value="False" /></settings>' -Encoding UTF8
    Set-Content -Path (Join-Path $dataDir 'DmtMagicWords.xml') -Value '<magicwords />' -Encoding UTF8
    New-Item -Path (Join-Path $dataDir 'DmtWallpaperPositions') -Force | Out-Null
    Set-Content -Path (Join-Path $dataDir 'DmtWallpaperPositions\1.dmtp') -Value 'x' -Encoding ASCII
    [System.IO.File]::WriteAllBytes((Join-Path $dataDir 'DmtWallpaper.bmp'), (New-Object byte[] 1024))

    # 5) 让壁纸指向数据目录，模拟 DMT 改过壁纸
    Set-ItemProperty -Path $desktopKey -Name 'Wallpaper' -Value (Join-Path $dataDir 'DmtWallpaper.bmp') -Type String

    Write-Host "已制造全部运行时残留" -ForegroundColor Yellow

    # --- 静默卸载 ---
    $unins = Join-Path $targetDir 'unins000.exe'
    Assert-True (Test-Path $unins) "卸载程序不存在"
    $u = Start-Process -FilePath $unins `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
        -Wait -PassThru
    Assert-True ($u.ExitCode -eq 0) "卸载失败，退出码 $($u.ExitCode)"

    # --- 断言全部消失 ---
    Assert-True (-not (Test-Path $dmtKey))     "HKCU\SOFTWARE\Dual Monitor Tools 子树未删除"
    Assert-True (-not (Test-Path $ctxKey))     "右键菜单子树未删除（只删了空键会留下子命令）"
    $runValue = (Get-ItemProperty $runKey -Name 'GNE_DualMonitorTools' -ErrorAction SilentlyContinue).GNE_DualMonitorTools
    Assert-True ($null -eq $runValue)          "开机自启值未删除"
    Assert-True (-not (Test-Path $dataDir))    "数据目录未删除"
    Assert-True (-not (Test-Path $ssKey))      "ScreenSaverInstalled 标记未删除"
    Assert-True (-not (Test-Path $scrPath))    "System32 下的屏保未删除"

    $w = (Get-ItemProperty $desktopKey -Name Wallpaper -ErrorAction SilentlyContinue).Wallpaper
    Assert-True ($w -eq '')                    "壁纸未被清空（实际值：'$w'）"

    Write-Host "卸载阶段 PASS：全部残留已清除" -ForegroundColor Green
}
finally {
    # --- 复原壁纸 ---
    Set-ItemProperty -Path $desktopKey -Name 'Wallpaper' -Value $origWallpaper -Type String
    if ($null -ne $origTileWallpaper)  { Set-ItemProperty -Path $desktopKey -Name 'TileWallpaper'  -Value $origTileWallpaper  -Type String }
    if ($null -ne $origWallpaperStyle) { Set-ItemProperty -Path $desktopKey -Name 'WallpaperStyle' -Value $origWallpaperStyle -Type String }

    # DWM 刷新壁纸
    $sig = @'
using System;
using System.Runtime.InteropServices;
public class Wallpaper {
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}
'@
    Add-Type -TypeDefinition $sig -ErrorAction SilentlyContinue
    [Wallpaper]::SystemParametersInfo(20, 0, $origWallpaper, 1) | Out-Null

    # --- 兜底清理 ---
    foreach ($p in @($dmtKey, $ctxKey, $dataDir, $targetDir)) {
        if (Test-Path $p) { Remove-Item -Path $p -Recurse -Force -ErrorAction SilentlyContinue }
    }
    if (Test-Path $ssKey) { Remove-Item -Path $ssKey -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $scrPath) { Remove-Item -Path $scrPath -Force -ErrorAction SilentlyContinue }

    Write-Host "已复原壁纸与清理测试残留" -ForegroundColor Yellow
}
```

- [ ] **Step 2: 跑测试确认失败**

Run:
```powershell
.\installer\tests\Test-Uninstall.ps1 -SetupExe .\dist\DualMonitorTools-2.13-zh-Hans-Setup.exe
```
Expected: FAIL 在 `HKCU\SOFTWARE\Dual Monitor Tools 子树未删除` 或 `右键菜单子树未删除`（此时 `[Code]` 段还没有卸载逻辑）

- [ ] **Step 3: 在 `[Code]` 段末尾追加卸载清理**

七步，每步独立 `try/except`，单步失败不中断后续。

```ini
const
	DmtProductKey = 'SOFTWARE\Dual Monitor Tools';
	DmtContextMenuKey = 'Software\Classes\directory\Background\shell\GNE_DualMonitorTools';
	AutoStartRunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
	AutoStartValueName = 'GNE_DualMonitorTools';
	DesktopKey = 'Control Panel\Desktop';
	DataFolderName = 'Dual Monitor Tools';

	SPI_SETDESKWALLPAPER = 20;
	SPIF_UPDATEINIFILE = 1;

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

		// 方案 (a)：清空为纯色，不指向可能缺失的 img0.jpg
		RegWriteStringValue(HKCU, DesktopKey, 'Wallpaper', '');
		SystemParametersInfoW(SPI_SETDESKWALLPAPER, 0, '', SPIF_UPDATEINIFILE);
		Log('cleared DMT-managed wallpaper: ' + Wallpaper);
	except
		Log('ClearWallpaperIfOurs failed');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
	dataDir: String;
begin
	if CurUninstallStep <> usPostUninstall then
		Exit;

	// 1-2 壁纸：仅当指向我们的数据目录时才清空
	ClearWallpaperIfOurs();

	// 3 界面语言与壁纸路径
	try
		RegDeleteKeyIncludingSubkeys(HKCU, DmtProductKey);
		Log('deleted HKCU\' + DmtProductKey);
	except
		Log('failed to delete HKCU\' + DmtProductKey);
	end;

	// 4 开机自启
	try
		RegDeleteValue(HKCU, AutoStartRunKey, AutoStartValueName);
		Log('deleted Run value ' + AutoStartValueName);
	except
		Log('failed to delete Run value');
	end;

	// 5 右键菜单整棵子树（含运行期生成的子命令）
	try
		RegDeleteKeyIncludingSubkeys(HKCU, DmtContextMenuKey);
		Log('deleted ' + DmtContextMenuKey);
	except
		Log('failed to delete context menu subtree');
	end;

	// 6 数据目录
	dataDir := ExpandConstant('{userappdata}\' + DataFolderName);
	try
		DelTree(dataDir, True, True, True);
		Log('deleted ' + dataDir);
	except
		Log('failed to delete ' + dataDir);
	end;

	// 7 更新下载残留
	try
		DeleteFile(ExpandStr('{tmp}\DualMonitorTools.msi'));
	except
		Log('failed to delete temp msi');
	end;
end;
```

注：最后一行若 `ExpandStr` 不是有效函数，改用 `ExpandConstant('{tmp}\DualMonitorTools.msi')`。Inno 的 `{tmp}` 在卸载时指向卸载器自己的临时目录，与原程序 `Path.GetTempPath()` 不是一个位置 —— 若编译或运行报错，把第 7 步改成删除 `GetEnv('TEMP') + '\DualMonitorTools.msi'`：

```ini
	try
		DeleteFile(GetEnv('TEMP') + '\DualMonitorTools.msi');
	except
		Log('failed to delete temp msi');
	end;
```

- [ ] **Step 4: 重新编译并跑测试确认通过**

Run:
```powershell
.\build-installer.ps1
.\installer\tests\Test-Uninstall.ps1 -SetupExe .\dist\DualMonitorTools-2.13-zh-Hans-Setup.exe
```
Expected: 输出 `安装阶段 PASS` + `卸载阶段 PASS：全部残留已清除` + `已复原壁纸与清理测试残留`

再确认基线干净：
```powershell
Test-Path 'HKCU:\SOFTWARE\Dual Monitor Tools'        # False
Test-Path 'HKCU:\Software\Classes\directory\Background\shell\GNE_DualMonitorTools'  # False
Test-Path (Join-Path $env:APPDATA 'Dual Monitor Tools')                              # False
Test-Path 'HKLM:\SOFTWARE\WOW6432Node\GNE'           # False
(Get-ItemProperty 'HKCU:\Control Panel\Desktop').Wallpaper  # 恢复为原值
```

- [ ] **Step 5: 提交**

```bash
git add installer/DualMonitorTools.iss installer/tests/Test-Uninstall.ps1
git commit -m "installer: clean up every runtime artefact on uninstall"
```

---

### Task 5: 文档与规则更新

**Files:**
- Create: `installer\README-installer.md`
- Modify: `AGENTS.md`（新增第 10 节；更新测试门槛）

**Interfaces:**
- Consumes: 前四个任务的产物
- Produces: 无（纯文档）

- [ ] **Step 1: 创建 `installer\README-installer.md`（UTF-8 无 BOM + LF）**

内容必须覆盖：构建方式、安装内容清单、注册表写入项、卸载清理项、**spec §9 的六条已知偏差**、验证方式、以及"行为测试需要管理员权限且会临时改注册表"的警告。

- [ ] **Step 2: 更新 `AGENTS.md`**

- 第 3 节构建方式增补：`.\build-installer.ps1` 产出 `dist\DualMonitorTools-2.13-zh-Hans-Setup.exe`。
- 第 5 节（运行时副作用）增补一句：安装包本身的写入项见 `installer\README-installer.md`。
- 新增第 10 节「安装包」，记录：Inno Setup 7 路径、`.iss`/`.xml` 编码要求、`installed` 标记禁忌（写它会导致自动更新覆盖中文版）、`[Code]` 段用 `SuppressibleMsgBox` 的原因、行为测试脚本位置与管理员权限要求。
- 把测试门槛更新为 `build.ps1 -Test` 输出的**实际**测试数（读命令输出，不要猜）。

- [ ] **Step 3: 验证 `AGENTS.md` 与 README 都提到了六条偏差**

Run:
```powershell
Select-String -Path installer\README-installer.md -Pattern '便携版安装|Repair|SysWOW64|runasoriginaluser|CHANGES' | Measure-Object | Select-Object -ExpandProperty Count
```
Expected: ≥ 5（六条偏差中至少有五条被提到）

- [ ] **Step 4: 提交**

```bash
git add installer/README-installer.md
git commit -m "docs: describe the installer, its deviations and how to verify it"
```

`AGENTS.md` 不入库（本地文件），不 `git add`。

---

## 收尾

全部任务完成后：

```powershell
.\build.ps1 -Clean
.\build.ps1 -Configuration Release -Test      # 全绿，Failed: 0
.\build-installer.ps1                          # 产出 dist\*.exe
git status --porcelain                         # 仅 dist/ 被忽略、无残留
git log --oneline -6
```

然后向用户报告：产物路径、测试数、六条偏差、以及行为验证是否实跑过（若无管理员权限跑不了 `installer\tests\*.ps1`，必须明确写"未验证"及原因）。
