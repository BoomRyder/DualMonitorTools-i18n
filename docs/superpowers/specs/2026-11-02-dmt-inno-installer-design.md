# DMT 安装/卸载程序（Inno Setup）设计

日期：2026-11-02
状态：待评审
前置：`2026-11-02-dmt-zh-hans-i18n-design.md`（已实现并合入 `main`）

## 1. 目标与不目标

### 1.1 目标

1. 用 **Inno Setup 7.1.0**（本机已装于 `D:\Program Files\Inno Setup 7\`）产出一个可分发的中文版安装包，输出 `DualMonitorTools-2.13-zh-Hans-Setup.exe`。
2. **完整复刻原 `DualMonitorTools-2.13.msi` 的全部安装行为**：同样的安装位置、同样的开始菜单快捷方式、同样的注册表项、同样的数据目录、同样的"装完启动程序"。
3. 卸载时**彻底清干净 DMT 运行时产生的一切**（`AGENTS.md` 第 5 节列出的注册表与文件），不留垃圾，不破坏系统共享值。
4. 安装包界面**中英双语，跟随系统区域**。

### 1.2 不目标

- 不追求与原 MSI 位级等价，也不做 MSI→Inno 的升级迁移（检测到原版就拒绝安装）。
- 不实现 MSI 的「修复（Repair）」和「修改（Modify）」功能。
- 不改动任何作者源码（硬约束 2）。因此**不写 `installed` 标记**这一决策带来的 UI 偏差只能接受，见 §9.1。
- 不新增代码签名（`ISSigTool.exe` 可用，但无证书，不在本设计范围）。
- 不做在线更新机制（原 MSI 靠 `installed` 标记启用自动更新，本方案主动关闭）。

## 2. 产物

| 路径 | 作用 |
|---|---|
| `installer\DualMonitorTools.iss` | Inno Setup 脚本，唯一安装工程 |
| `installer\DmtFileLocations.xml` | 数据目录重定向文件，安装到程序目录 |
| `installer\README-installer.md` | 构建说明与设计摘要（面向维护者） |
| `build-installer.ps1` | 一键构建：先 `build.ps1 -Configuration Release`，再调 `ISCC.exe` |

编译命令（`build-installer.ps1` 内部）：

```
& "D:\Program Files\Inno Setup 7\ISCC.exe" `
    /DMyAppVersion=2.13 `
    /DRepoRoot="<仓库根绝对路径>" `
    /O"<仓库根>\dist" `
    installer\DualMonitorTools.iss
```

`ISCC.exe` 路径通过注册表 `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1\InstallLocation` 探测，失败则回退到 `D:\Program Files\Inno Setup 7\`、`C:\Program Files (x86)\Inno Setup 7\`、`C:\Program Files\Inno Setup 7\`，最后才报错并提示用户设置 `INNO_SETUP_DIR` 环境变量。

## 3. 安装包形态

| 项 | 值 | 依据 |
|---|---|---|
| `AppId` | `{91A49D81-5FF8-421E-A669-94A906D5D700}` | 本设计新分配，用于 Inno 自身的升级识别 |
| `AppName` | `Dual Monitor Tools` | 与原 MSI `ProductName` 一致 |
| `AppVersion` | `2.13` | |
| `AppVerName` | `Dual Monitor Tools 2.13 (简体中文)` | 便于与原版区分 |
| `AppPublisher` | `GNE` | 与原 MSI `Manufacturer` 一致 |
| `AppPublisherURL` | `http://dualmonitortool.sourceforge.net/` | 源码中 `AppForm` 的官网链接 |
| `DefaultDirName` | `{autopf}\Dual Monitor Tools` | 32 位包下 `{autopf}` 实测 = `C:\Program Files (x86)`，与原 MSI `ProgramFilesFolder` 一致 |
| `DefaultGroupName` | `Dual Monitor Tools` | 与原 MSI `ProgramMenuFolder\Dual Monitor Tools` 一致 |
| `PrivilegesRequired` | `admin` | 需写 `HKLM` 与 `System32` |
| `ArchitecturesAllowed` | `x64compatible` | 仅支持 64 位 Windows |
| `ArchitecturesInstallIn64BitMode` | **不设置** | 保持 32 位包，`HKLM\Software\...` 自动落入 `WOW6432Node`，与原 MSI（`Template=Intel;1033`）一致 |
| `OutputDir` | 由 `/O` 指定为 `dist\` | |
| `OutputBaseFilename` | `DualMonitorTools-2.13-zh-Hans-Setup` | |
| `Compression` / `SolidCompression` | `lzma2/max` / `yes` | |
| `SetupIconFile` | `..\DMT\DMT\DMT.ico` | 15086 B，源码已带 |
| `UninstallDisplayIcon` | `{app}\DMT.exe` | |
| `LicenseFile` | `..\COPYING.txt` | 复刻原 MSI 的 EULA 页（`WixUIPrintEula`）；GPLv3 全文 35821 B |
| `DisableProgramGroupPage` | `yes` | 分组名固定，不提供修改 |
| `AllowNoIcons` | `no` | 与原 MSI 一致，强制建快捷方式 |
| `CloseApplications` | `yes` | 装/卸前自动请求关闭占用文件的应用 |
| `RestartApplications` | `no` | 不自动重启 |
| `SetupLogging` | `yes` | 便于用户反馈故障 |

### 3.1 语言

```ini
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
```

`ChineseSimplified.isl` 是 Inno 7 内置（21516 B，头注 `Inno Setup version 6.5.0+`）。Inno 会**按系统区域自动选择**第一个匹配的语言（`LanguageDetectionMethod=uilanguage` 默认值），无需额外代码。

## 4. 文件布局

### 4.1 程序目录 `{app}` = `C:\Program Files (x86)\Dual Monitor Tools\`

| 来源 | 说明 |
|---|---|
| `..\DMT\DMT\bin\Release\DMT.exe` | 主程序 1002496 B |
| `..\DMT\DMT\bin\Release\DMT.exe.config` | 182 B，用户明确要求装上 |
| `..\DMT\DMT\bin\Release\zh-Hans\DMT.resources.dll` | 67584 B，**必需**，缺了中文界面失效 |
| `..\DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe` | 140288 B |
| `..\DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe.config` | 159 B |
| `..\DualWallpaper\DualWallpaper\bin\Release\zh-Hans\DualWallpaper.resources.dll` | 8704 B，**必需** |
| `..\DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe` | 27136 B |
| `..\DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe.config` | 182 B |
| `..\DmtWallpaper\DmtWallpaper\bin\Release\zh-Hans\DmtWallpaper.resources.dll` | 5120 B，**必需** |
| `installer\DmtFileLocations.xml` | 新增，见 §6 |
| `..\COPYING.txt` | 35821 B |
| `..\README.md` | 23396 B |

三个 `zh-Hans\*.resources.dll` 是本设计**相对原 MSI 新增**的必需件（原 MSI 无 i18n 子系统）。

### 4.2 系统目录 `{sysnative}` = `C:\Windows\System32\`

| 来源 | 说明 |
|---|---|
| `..\DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.scr` | 32256 B |

**明确 deviate 原 MSI**：原 MSI 装到 `SystemFolder`（32 位包下解析为 `SysWOW64`），导致屏保不出现在 64 位 `desk.cpl` 的屏保下拉列表里。本设计装到 `System32`。实测 `System32` 下有 6 个 `.scr`、`SysWOW64` 下只有 3 个，且 `desk.cpl` 是 x64，可佐证。

**关键坑（实现时实测发现，初稿判断有误）**：32 位安装包用 `DestDir: "{sys}"` **不会**落到真正的 `System32` —— WOW64 文件系统重定向会把它重定向到 `C:\Windows\SysWOW64`。实测证据：首轮安装后 `System32\DmtWallpaper.scr` 不存在、`SysWOW64\DmtWallpaper.scr` 存在（32768 B），与原 MSI 的缺陷完全一样。必须改用 `DestDir: "{sysnative}"`（Inno 提供的免重定向常量，已实测在 Inno 7 下编译并落地到真正的 System32）。`IssInstallsScreenSaverIntoTheRealSystem32` 与 `Test-Uninstall.ps1` 的双向断言现在钉住这一点。

### 4.3 开始菜单 `{group}`

| 快捷方式 | 目标 |
|---|---|
| `Dual Monitor Tools` | `{app}\DMT.exe` |
| `DualWallpaper` | `{app}\DualWallpaper.exe` |

与原 MSI 的 `Shortcut` 表一致。`{group}` 由 Inno 自动 `uninsdelete`。

## 5. 注册表

### 5.1 写入项（随卸载自动处理）

| 根 | 键 | 值 | Flags | 与原 MSI |
|---|---|---|---|---|
| `HKLM` | `Software\GNE\Dual Monitor Tools` | `ScreenSaverInstalled` = DWORD `1` | `uninsdeletekey` | ✅ 一致（原 MSI 也写，源码中无任何读写，纯标记）。`uninsdeletevalue` 只删值、会留下空键，故改用 `uninsdeletekey` |
| `HKCU` | `Software\Classes\directory\Background\shell\GNE_DualMonitorTools` | （空键） | `uninsdeletekey` | ✅ 一致 |

因 `ArchitecturesInstallIn64BitMode` 未设，`HKLM\Software\GNE\...` 在 64 位系统上实际落到 `HKLM\SOFTWARE\WOW6432Node\GNE\...` —— 已实测，与原 MSI 相同。

### 5.2 明确不写入

| 项 | 原因 |
|---|---|
| `HKLM\...\GNE\Dual Monitor Tools\installed = 1` | **核心决策**。若写，`GeneralModule.IsMsiInstall` 为真 → `CheckUpdatesForm` 显示"安装新版本"按钮 → 实测 `http://dualmonitortool.sourceforge.net/LatestVersion.xml` 仍可达，会下载并运行**英文原版 MSI**，覆盖中文版。代价见 §9.1。 |

## 6. 数据目录与 DmtFileLocations.xml

### 6.1 数据目录

沿用原 MSI：**`%APPDATA%\Dual Monitor Tools\`**（即 `{userappdata}\Dual Monitor Tools`）。

依据：`DMT\DMT\Library\Settings\FileLocations.cs` 的 `DataDirectory` 默认是 exe 同目录（便携形态），但 exe 同目录若存在 `DmtFileLocations.xml` 且其中 `datadirectory` 有值则重映射。原 MSI 装的正是这样一份文件。

**安装程序不预先创建该目录**。`FileLocations.cs:139-148` 在首次运行时 `Directory.CreateDirectory(DataDirectory)`，由程序自己建，避免以提权身份创建 per-user 目录导致的属主问题。

### 6.2 文件内容

`installer\DmtFileLocations.xml`（UTF-8 **with BOM**，`README.md:226` 要求）：

```xml
<?xml version="1.0" encoding="utf-8"?>
<!--
    This file tells Dual Monitor Tools where to store its data files.
    It must reside in the same directory as DMT.exe and is not written to
    by the program, so it is safe under C:\Program Files (x86).
-->
<filelocations>
    <filelocation name="datadirectory" value="%APPDATA%\Dual Monitor Tools" />
</filelocations>
```

解析器是 `FileLocations.GetLocationRemaps`：`XmlTextReader` 逐节点找 `LocalName == "filelocation"`，读 `name`/`value` 属性，`value` 走 `Environment.ExpandEnvironmentVariables`。因此 BOM、注释、缩进、元素名大小写都不敏感，只要求属性名恰为 `name`/`value`。

### 6.3 数据目录中会被程序生成的文件（卸载需清）

`DmtSettings.xml`、`DmtSettings.bak`、`DmtMagicWords.xml`、`DmtMagicWords.bak`、`DmtWallpaperProviders.xml`、`DmtWallpaper.bmp`（或 `.png`/`.jpg`，取决于壁纸格式）、`DmtWallpaperPositions\*.dmtp\`。

卸载按目录整体删除，不逐个枚举。

## 7. 卸载流程

分两个阶段，顺序固定。

### 7.1 阶段一：Inno 自动处理

- `{app}` 下所有已安装文件（含 `zh-Hans\` 子目录）
- `{sys}\DmtWallpaper.scr`
- `{group}` 整个开始菜单文件夹
- 注册表 §5.1 的两条（`uninsdeletevalue` / `uninsdeletekey`）
- Inno 自己的 `unins000.exe/-.dat` 与 `HKLM` 卸载项

### 7.2 阶段二：`[Code]` 的 `CurUninstallStepChanged(usPostUninstall)`

Inno 管不到的部分，按以下顺序执行，**每一步独立 try/except，单步失败不中断后续步骤**（否则用户会卡在半卸载状态）：

| # | 动作 | 实现 | 为什么必须手工 |
|---|---|---|---|
| 1 | 读 `HKCU\Control Panel\Desktop\Wallpaper`，判断是否位于 `{userappdata}\Dual Monitor Tools\` 之下（大小写不敏感前缀匹配） | `RegQueryStringValue(HKCU, 'Control Panel\Desktop', 'Wallpaper', W)` + `CompareText(Copy(W,1,Length(D)), D) = 0` | 只有 DMT 用壁纸变换器改过壁纸时才会指向该目录 |
| 2 | 命中则清空壁纸：`Wallpaper` 写空串，再 `SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, nil, SPIF_UPDATEINIFILE)` | `RegWriteStringValue` + `external 'SystemParametersInfoW@user32.dll stdcall'`；`SPI_SETDESKWALLPAPER = $0014`、`SPIF_UPDATEINIFILE = $01` | 采用方案 (a)：桌面回到纯色。不指向 `img0.jpg`，因精简系统可能缺该文件 |
| 3 | `RegDeleteKeyIncludingSubkeys(HKCU, 'SOFTWARE\Dual Monitor Tools')` | 整棵子树 | 清 `Language`、`DmtWallpaperPath`。程序运行期产生，Inno 不知道其存在 |
| 4 | 删 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 的**值名** `GNE_DualMonitorTools` | `RegDeleteValue` | 同上（"开机自启"开关打开的产物） |
| 5 | `RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\directory\Background\shell\GNE_DualMonitorTools')` | 整棵子树 | **陷阱 1**：右键菜单开关一旦打开会生成 11 条子命令（Options/MagicWord/...），只删空键会全部残留 |
| 6 | `DelTree(ExpandConstant('{userappdata}\Dual Monitor Tools'), True, True, True)` | 递归删目录 | 用户选择"用户数据全部删除" |
| 7 | 删 `%TEMP%\DualMonitorTools.msi`（若存在） | `DeleteFile(ExpandConstant('{tmp}\DualMonitorTools.msi'))` | 原程序的"检查更新"下载残留 |

**关于 §7.2 第 1 步的顺序**：原设计稿曾把"先读出壁纸路径"单列为第 1 步、理由是"删目录后就查不到"。实际上我们只读注册表里的**字符串**，删除目录不影响该值，因此读与判可以合并为一步。这里已简化。

**为什么不删 `HKCU\Control Panel\Desktop` 的 `TileWallpaper`/`WallpaperStyle`**：这两个是 Windows 全局设置，用户自己也可能改过，`AGENTS.md` 5.3 明确列为禁止删除。第 2 步的 `SystemParametersInfo` 会由系统重算这两个值。

## 8. 前置检查

全部在 `[Code]` 的 `InitializeSetup()` 中执行，任一不满足则 `Result := False` 并弹窗说明，不进入安装向导。

| # | 检查 | 依据 | 失败提示 |
|---|---|---|---|
| 1 | .NET Framework 4.x | `SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client` 的 `Install` DWORD = 1，**64 位视图与 `WOW6432Node` 视图都查** | 需先安装 .NET Framework 4.0 或更高版本 |
| 2 | 旧 MSI 版是否已安装 | 以下四处任一存在即判为已装：`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{59CFB0C1-3F5B-49B6-B1C9-C473153C1257}`、其 `WOW6432Node` 对应项、`HKLM\SOFTWARE\GNE\Dual Monitor Tools\installed`、`HKLM\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools\installed` | 检测到原版 MSI 安装，请先在"程序和功能"中卸载 |
| 3 | `DMT.exe` 未在运行 | `FindWindowByWindowName('DMT_GUI_WINDOW') <> 0` | 请先退出托盘中的 DMT 再安装 |

**实现选择（相对初稿已简化）**：初稿打算用 `CreateToolhelp32Snapshot` + `Process32First/Next` 枚举进程名，约 60 行脆弱 Pascal。实际不需要：

- **文件占用**由 `[Setup] CloseApplications=yes` 覆盖。它走 Restart Manager，在文件被占用时提示用户关闭，**不依赖命名 mutex**（初稿把 `CloseApplications` 和 `AppMutex` 混为一谈，是错的：DMT 确实没有命名 mutex，但 Restart Manager 不需要它）。
- **DMT 是否在跑**用一行 `FindWindowByWindowName('DMT_GUI_WINDOW')` 即可。`AppForm` 的 `this.Text = "DMT_GUI_WINDOW"` 是不可翻译常量（`NoLiteralRemains_Test` 已在守卫白名单里钉死它），`FindWindow` 对隐藏窗口同样生效。
- `DualWallpaper.exe` 不做单独检查：它的窗口标题取自 `Program.MyTitle`（`Resources.MyTitle`，已 i18n 可翻译），没有稳定常量可比对；且它只读壁纸文件，不持有安装目标目录中的任何文件，`CloseApplications` 足够。

## 9. 已知偏差（必须写进 README-installer.md）

### 9.1 设置面板显示"便携版安装"

不写 `installed` 标记 → `GeneralModule.IsMsiInstall` 返回 false → `GeneralOptionsPanel.cs:63` 显示 `PortableInstall`（"便携版安装" / "Portable installation"），尽管它确实是安装版。

这是关闭自动更新的必然代价。修正它需要改 `GeneralModule.cs`，违反硬约束 2（不改作者源码），因此**接受**。

### 9.2 无「修复」功能

Inno 无 MSI 的 Repair。文件损坏只能卸载重装。

### 9.3 与原 MSI 不能共存也不自动升级

检测到原 MSI 版直接拒绝安装。也不做 Inno→MSI 反向迁移。

### 9.4 `.scr` 位置变更

原版装 `SysWOW64`（屏保列表里看不到），本方案装 `System32`。属**修正**而非退化，但改变了文件位置，卸载时只删 `System32` 下那份。

### 9.5 文档文件合并

原 MSI 装 `CHANGES.txt`/`README.txt`/`THANKS.txt`，本仓库已将四份文档合并为 `README.md`，因此只装 `README.md` + `COPYING.txt`。

### 9.6 装完启动使用 `runasoriginaluser`

原 MSI 的 `StartAppOnExit`（Type=210）在提权上下文里启动 `dmt.exe`，程序以管理员身份运行。本设计用 `[Run]` 的 `Flags: postinstall nowait skipifsilent runasoriginaluser`，**以原用户身份**启动，更安全。这是有意的改进。

## 10. 文件编码约定

沿用 `AGENTS.md` 第 6 节：

- `.iss`：Inno 要求 UTF-8；若含中文则**必须 with BOM**（Inno 6+ 支持无 BOM 的 UTF-8，但 PowerShell 5.1 写入时用 `UTF8Encoding($false)` 会剥 BOM，容易混淆）。本设计统一 `.iss` 与 `.xml` 用 **UTF-8 with BOM**，`.md` 用 UTF-8 无 BOM + LF。
- 所有中文消息字符串直接写在 `.iss` 里，通过 `CustomMessage` 或 `[Messages]` 段落覆盖，不依赖 `.isl` 文件的翻译质量（`ChineseSimplified.isl` 只负责向导骨架）。

## 11. 验证方式

由于 `AGENTS.md` 第 4 节硬约束「不启动 GUI 做验证」，本轮验证**不双击运行安装包**（除用户明确要求）。可自动化的验证：

1. **编译通过**：`build-installer.ps1` 退出码 0，产出 `dist\DualMonitorTools-2.13-zh-Hans-Setup.exe`。
2. **内容核对**：`ISCC` 生成的 `.iss` 编译日志 + 用 7-Zip/`innounp` 解包 Setup.exe 核对文件清单与目标路径（若工具不可得，退化为核对 `[Files]` 段与源文件存在性）。
3. **脚本静态检查**：`[Code]` 段用 `ISCC` 编译即等于语法检查（Inno 编译期会解析 Pascal 脚本）。

### 11.1 已完成的预验证（本设计提交前实测）

用一个最小 `.iss`（`PrivilegesRequired=admin`、`ArchitecturesAllowed=x64compatible`、双语言、`[Code]` 里调用本节用到的全部函数）跑 `ISCC.exe`：

```
Reading file: D:\Program Files\Inno Setup 7\Languages\ChineseSimplified.isl
Reading [Code] section
Compiling [Code] section
Successful compile (0.859 sec).
```

即以下均已确认可用，不是假设：

- `ChineseSimplified.isl` 在 Inno Setup 7 下正常加载
- `RegDeleteKeyIncludingSubkeys(HKCU, ...)`
- `DelTree(path, True, True, True)`
- `RegWriteStringValue` / `RegDeleteValue`
- `PrivilegesRequired=admin` + `ArchitecturesAllowed=x64compatible` 组合

探测目录已删除，未在系统留下任何注册表或文件。
4. **注册表/卸载逻辑**：用 `/VERYSILENT /DIR=<临时目录>` 在临时目录静默安装 → 检查注册表与文件 → 静默卸载 → 复查注册表与目录已复原。（这一步会真实写 HKLM 与 `%APPDATA%`，需在验证后清理；`AGENTS.md` 5.9 基线已记录本机原始状态。）
5. **不验证**：安装向导的中英文界面外观、屏保是否出现在系统列表（需 GUI）。

## 12. 风险

| 风险 | 缓解 |
|---|---|
| `SystemParametersInfo` 清空壁纸在部分系统上不生效，桌面仍指向已删除的文件 | 同时把 `Wallpaper` 注册表值写空串，双保险。残留表现为桌面黑底，用户可自行重设 |
| 提权安装后 `%APPDATA%\Dual Monitor Tools` 属主问题 | 安装程序不创建该目录，由程序以普通用户身份创建 |
| 用户装了原版 MSI 但卸载项被第三方清理工具删掉，导致检测漏判 | `GNE\installed` 标记是第二条检测线，两者都查 |
| ~~Inno 7 的 `ChineseSimplified.isl` 与 6.x 消息键不兼容~~ | **已排除**：用一个最小 `.iss` 实测编译成功（见 §11.3） |
| `DelTree` 删除正在使用的数据文件失败 | 前置于 §8 第 3 项的进程检查；卸载时 `CloseApplications=yes` 也会尝试关闭 |
