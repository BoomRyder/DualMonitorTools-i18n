# 运行时产物清单 — 供卸载程序使用

本文记录 **DMT 运行后会生成的注册表项与文件**，以及卸载时必须清理、必须保留、必须恢复的三类边界。所有结论均标注源码位置（`DMT\DMT\...` 指 `DualMonitorTools-2.13-src\DMT\DMT\...`）或 MSI 表名，便于复核。

范围：`DMT.exe`（主程序）、`DualWallpaper.exe`、`DmtWallpaper.scr`（屏保）三个产物的全部持久化副作用，以及原版 `DualMonitorTools-2.13.msi` 安装期写入的项。

---

## 0. 先明确一个前提：数据目录在哪

DMT 的配置文件位置由 `Library\Settings\FileLocations.cs` 决定：

- 默认 `DataDirectory = exe 所在目录`（`FileLocations.cs:118-121`，注释写明"to simplify portable usage"）。
- 若 exe 同目录存在 `DmtFileLocations.xml`（`FileLocations.cs:124`），则其中 `datadirectory` 条目覆盖它（`:132-135`），且支持环境变量展开（`:193`）。

**关键**：原版 MSI 会**安装一个 `DmtFileLocations.xml`**（MSI `File` 表：`FileLocations.xml` → 目标名 `DmtFileLocations.xml`，装入 `APPLICATIONROOTDIRECTORY`），内容为：

```xml
<filelocation name="datadirectory" value="%APPDATA%\Dual Monitor Tools" />
```

所以两种部署形态的数据目录完全不同：

| 形态 | 数据目录 |
|---|---|
| MSI 安装 | `%APPDATA%\Dual Monitor Tools\` |
| 免安装（无 FileLocations.xml） | `DMT.exe` 所在目录 |

**卸载程序必须在删除任何东西之前先解析这个路径**，因为 `DmtFileLocations.xml` 本身会被 MSI 卸掉，之后就无法再定位数据目录。解析顺序照抄 `FileLocations.cs`：有 `DmtFileLocations.xml` 就读 `datadirectory`，否则用 exe 目录。

---

## A. DMT 运行时写入的注册表（**卸载必须清理**）

全部在 `HKCU`，不需要管理员权限。

### A1. 壁纸路径

| 项 | 值 |
|---|---|
| 键 | `HKCU\SOFTWARE\Dual Monitor Tools\DMT` |
| 名 | `DmtWallpaperPath`（REG_SZ） |
| 内容 | 最近一次生成的壁纸文件绝对路径 |

- 写入点：`Library\Environment\DmtRegistry.cs:11-24`（`DmtKey` 在 `:13`，值名在 `:14`，`CreateSubKey` + `SetValue` 在 `:20-21`）
- 触发：每次成功设置壁纸（`Library\Wallpaper\WindowsWallpaper.cs:186`）
- 写入者：`DMT.exe` 和 `DualWallpaper.exe`（后者经共享的 `WindowsWallpaper` 间接写入）
- 读者：`DmtWallpaper.scr`（`DmtWallpaper\WallpaperFilenameHelper.cs:62`，只读）

卸载动作：`HKCU\SOFTWARE\Dual Monitor Tools` 整棵子树可以删（该分支下只有 DMT 自己的东西）。注意它**不是** MSI 装的（MSI 装的是 `HKLM\Software\GNE\Dual Monitor Tools`，见 D 节），MSI 不会碰它。

### A2. 开机自启

| 项 | 值 |
|---|---|
| 键 | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| 名 | `GNE_DualMonitorTools`（REG_SZ） |
| 内容 | `DMT.exe` 绝对路径 |

- 写入点：`Library\Environment\AutoStart.cs:67-68`；删除点 `:80-81`（`DeleteValue`）；探测 `:45`
- 触发：用户在 Options 里勾选"随 Windows 启动"

卸载动作：**只删这一个值名，不要删 Run 键本身。** `AutoStart.cs:80` 自己就是这么做的。

### A3. 桌面右键菜单（整棵子树）

| 项 | 值 |
|---|---|
| 根 | `HKCU\Software\Classes\directory\Background\shell\GNE_DualMonitorTools` |
| 内容 | 一棵子树：根键 4 个值（`MUIVerb`、默认图标=`DMT.exe` 路径、`Position`、`SubCommands=""`）+ 11 条子命令 + 部分子命令下的 `command` 子键 |

- 常量：`Library\Environment\DesktopContextMenu.cs:36`（`ContextMenuRootKey`）、`:38`（`DMTKeyName`）
- `Add()` 建树在 `:131-193`；根键 4 个值在 `:141-144`；`cursorKey` 在 `:180-183`；通用子键/命令写在 `:215-227`（`:217` `key.SetValue("", keyValue)`、`:221` `CommandFlags` DWORD、`:227` `commandKey.SetValue("", cmd)`）
- `Remove()` 在 `:202-210`，用 `DeleteSubKeyTree(DMTKeyName, false)`（`:208`）
- 探测 `IsActive()` 在 `:109-129`

卸载动作：`DeleteSubKeyTree` 整棵树。**见 H 节陷阱 1 —— MSI 自己删不掉它。**

---

## B. 系统共享注册表值（**禁止删除，只能恢复**）

这一节是卸载程序最容易搞坏用户机器的地方。

### B1. 壁纸样式

| 项 | 值（DMT 写入） |
|---|---|
| 键 | `HKCU\Control Panel\Desktop` |
| 名 | `TileWallpaper` = `"1"`、`WallpaperStyle` = `"0"` |

- 写入点：`Library\Wallpaper\WindowsWallpaper.cs:356-359`（`SetTiledWallpaper()`，`OpenSubKey(@"Control Panel\Desktop", true)` 后 `SetValue`）
- 触发：**每次设壁纸都无条件写**，与用户选的显示方式无关（原作者实现较粗暴）

**这些是 Windows 系统值，不属于 DMT。卸载时绝不能删这两个值名** —— 删掉会让桌面壁纸配置进入未定义状态。正确做法是**记录安装前的原值、卸载时恢复**，或干脆不动并提示用户自行调整。

### B2. 当前壁纸路径（间接）

设壁纸时 `NativeMethods.SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE)`（`WindowsWallpaper.cs:366`）会连带影响：

- `HKCU\Control Panel\Desktop\Wallpaper`
- `%APPDATA%\Microsoft\Windows\Themes\TranscodedWallpaper`（Win10+ 的系统副本）

这些由 Windows 维护，**不要删**。但会引出 H 节陷阱 2：卸载后壁纸指向已删除文件。

本机当前实测值（从未跑过 GUI，故为 Windows 原始状态，可作对照基线）：`Wallpaper = ...\Themes\TranscodedWallpaper`、`WallpaperStyle = 10`、`TileWallpaper = 0`。

---

## C. 运行时生成的配置文件（**卸载时征询后清理**）

位置见第 0 节。以下文件名默认值来自 `Library\Settings\FileLocations.cs:154-158`，均可被 `DmtFileLocations.xml` 单独重映射。

| 文件/目录 | 内容 | 何时产生 |
|---|---|---|
| `DmtSettings.xml` | 全部模块设置，条目格式 `<setting name="模块__项" value="值"/>` | **首次运行即产生**（`Controller.cs:102-106` 的 `FirstRun` 设置）；之后任一设置项被改动立刻重写 |
| `DmtMagicWords.xml` | Launcher 魔术字 | 正常退出时必写（`LauncherModule.cs:339-341`） |
| `DmtWallpaperProviders.xml` | 壁纸源配置 | 配置壁纸源后 |
| `DmtWallpaper.bmp`<br>`DmtWallpaper.png`<br>`DmtWallpaper.jpg` | 生成的壁纸图像副本 | 每次设壁纸。**扩展名三选一**，由设置项 `WallpaperImageFormat` 决定（`WindowsWallpaper.cs:270-296`，`Path.ChangeExtension`）；BMP 是 fallback（`:291-295`）。**卸载要三个都查** |
| `DmtWallpaperPositions\` 目录下的 `*.dmtp` | 轮播进度，二进制 | 启用持久化轮播后（`PositionTrackerRepository.cs:44-50` 拼 `key + ".dmtp"`；`PositionTracker.cs:286` 写入、`:332` 删除、`:343` 建目录） |
| `*.tmp` / `*.bak` | 写入中间件残留 | `Library\Utils\SafeFileWriter.cs:45-88` 的固定行为：写 `.tmp` → 原文件改名 `.bak` → tmp 转正。`.bak` 会留存 |
| 日志文件（默认不存在） | 调试日志 | 仅当 `DmtFileLocations.xml` 显式配了 `log`（`FileLocations.cs:161` 默认 `null`） |

设置项的 setter 内部直接调 `SaveSettings()`（`BoolSetting.cs:64-69`、`IntSetting.cs:68`、`UIntSetting.cs:68`、`StringSetting.cs:67`），所以不是"退出才存"，删文件前要先确认进程已退出。

卸载动作：整个数据目录属于用户数据（含自定义热键、魔术字、壁纸源配置）。建议默认**保留**并提示，或提供"同时删除个人设置"选项；不要静默删除。

---

## D. 安装期写入、由 MSI 自己清理的项（**仅供核对**）

这些是原版 MSI 装的，不在运行时产生，正常的 MSI 卸载会处理掉。列出来是为了让新的卸载程序能核对"是否残留"。

| 项 | 内容 | MSI 来源 |
|---|---|---|
| `HKLM\Software\GNE\Dual Monitor Tools` → `installed` = `1` | 标记 MSI 安装 | `Registry` 表 `reg26BA37B8...`，组件 `DmtShortcut`，Root=-1 + `ALLUSERS=1` → HKLM |
| `HKLM\Software\GNE\Dual Monitor Tools` → `ScreenSaverInstalled` = `1` | 标记屏保已装 | `Registry` 表 `regEFD6EF57...`，组件 `DualWallpaperShortcut` |
| `HKCU\Software\Classes\directory\Background\shell\GNE_DualMonitorTools`（空键占位） | 为卸载预留 | `Registry` 表 `DmtContextMenuReg`，Root=1（HKCU），无名值 —— **见陷阱 1** |
| `C:\Program Files (x86)\Dual Monitor Tools\` 全部文件 | 程序本体 | `Directory`/`File` 表 |
| `%ProgramData%\Microsoft\Windows\Start Menu\Programs\Dual Monitor Tools\` 两个快捷方式 | DMT、DualWallpaper | `Shortcut` + `RemoveFile` 表（InstallMode=2，整目录删） |
| `SystemFolder\DmtWallpaper.scr` | 屏保 | `File` 表，组件 `DmtWallpaper.scr` |

关于第 1、2 条的实际落点：`Registry` 表 Root=-1 且 `ALLUSERS=1`，因此写 HKLM；但 MSI 是 32 位包（SummaryInformation `Template = Intel;1033`），无 64 位组件标记，所以在 x64 上实际落在 **`HKLM\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools`**。原作者也拿不准，所以 `Modules\General\GeneralModule.cs:152/161` 两处都读（`InstalledKeyName` / `Installed6432KeyName`，`:50-52`）。**卸载时两处都要删并核对。**

`SystemFolder` 同理：32 位 MSI 在 x64 上可能重定向到 `C:\Windows\SysWOW64`。卸载时 `System32` 与 `SysWOW64` 都要检查 `DmtWallpaper.scr` 是否残留。

---

## E. 临时文件

| 路径 | 何时产生 |
|---|---|
| `%TEMP%\DualMonitorTools.msi` | 用户使用"检查更新→下载"时（`Modules\General\GeneralModule.cs:185` 拼路径，`CheckUpdatesForm.cs:199-202` 写入并 `Process.Start`） |

卸载动作：删掉即可，无风险。

---

## F. 不产生持久化的副作用（无需清理，仅备忘）

- 全局热键 `RegisterHotKey`：进程退出即释放，不落注册表。
- 亮度/对比度（WMI、dxva2/DDC-CI）：改的是显示器硬件状态，DMT 不记录，卸载无法回滚。
- 单实例机制：`FindWindow(null, "DMT_GUI_WINDOW")` + `WM_COPYDATA`（`Library\Command\CommandMessaging.cs:164`）。**不建 mutex、不写 lock 文件。**
- 无 Startup 文件夹项、无 `.lnk`（除 D 节 MSI 装的两个开始菜单快捷方式）、无服务、无计划任务。
- 三个 `Properties\Settings.settings` 均为空，全仓无 `ConfigurationManager` 使用 → **不产生 `user.config`**。
- `G:\Wallpaper\<guid>.jpg`（`Library\Html\HttpRequester.cs:330-338`）包在 `#if DEBUG` 内，Release 不存在此行为。
- `Library\DisplayHandling\DisplayDevices.cs:249-250` 的 `ChangeDisplaySettingsEx(CDS_UPDATEREGISTRY)` 与 `:334-352` 的 `SetDisplayConfig(SDC_APPLY)` 会持久化显示配置，但 **Modules 层无任何调用点**，当前版本不会触发。若将来接上，卸载需考虑恢复。

---

## G. 建议的卸载顺序

1. 结束 `DMT.exe` / `DualWallpaper.exe` / `DmtWallpaper.scr` 进程（屏保可能由系统加载）。
2. **先解析数据目录**（第 0 节），把它记下来再动任何文件。
3. 删除 A1 / A2 / A3 三处运行时注册表。A2 只删值名，A3 用 `DeleteSubKeyTree`。
4. 检查 D 节两个 HKLM 键是否已被 MSI 清掉，残留则删（含 `WOW6432Node` 分支）。
5. 检查 `System32` 与 `SysWOW64` 下的 `DmtWallpaper.scr`。
6. 处理壁纸：若 `HKCU\Control Panel\Desktop\Wallpaper` 或 `DmtWallpaperPath` 指向即将删除的数据目录，先按陷阱 2 处理。
7. 若 `SCRNSAVE.EXE` 指向本产品的 `.scr`，按陷阱 3 处理。
8. 按用户选择决定是否删除数据目录（C 节）。**不要动 B1 的两个系统值，除非有安装前的备份可恢复。**
9. 删 `%TEMP%\DualMonitorTools.msi`。
10. 最后复核：A、D、E 全部不存在；B1 保持可用值。

---

## H. 已知陷阱

**陷阱 1：MSI 删不掉运行时生成的右键菜单子树。**
MSI `Registry` 表里 `DmtContextMenuReg` 只声明了一个"空键占位"（无名值、无值数据）。MSI 卸载时只删除**它自己创建且已为空**的键，不会递归删子键。而 DMT 运行后会在该键下生成 11 条子命令 + `command` 子键，导致 MSI 卸载时删除失败、整棵子树残留。这正是"卸载不干净"的典型来源，新卸载程序必须显式 `DeleteSubKeyTree`。

**陷阱 2：卸载后壁纸可能指向已删除的文件。**
DMT 把生成的壁纸存进数据目录并设为系统壁纸。若卸载时删了数据目录却没换壁纸，Windows 的壁纸路径就悬空了。建议：卸载前若检测到当前壁纸来自本数据目录，先恢复到系统默认或提示用户选择新壁纸。注意 DMT 自身**没有任何恢复原壁纸的逻辑**（全仓 grep `OriginalWallpaper` / `RestoreWallpaper` 无匹配），所以不能指望它自己收拾。

**陷阱 3：`SCRNSAVE.EXE` 可能悬空。**
用户若在 Windows 屏保设置里选中了 `DmtWallpaper.scr`，系统会把路径写进 `HKCU\Control Panel\Desktop\SCRNSAVE.EXE`。卸载删掉 `.scr` 后该值悬空。这不是 DMT 写的（源码全仓无 `SCRNSAVE` 写入），但由本产品引起。建议检测到指向本产品时清空该值。

**陷阱 4：`.bak` / `.tmp` 不是垃圾。**
`SafeFileWriter` 的设计就是留 `.bak`。清理数据目录时一并删没问题，但别把"存在 .bak"当成安装损坏。

**陷阱 5：壁纸文件扩展名是三选一。**
不要只删 `DmtWallpaper.bmp`。用户可能选了 PNG/JPG，实际文件是 `DmtWallpaper.png` / `.jpg`。

**陷阱 6：设置是即时落盘的。**
setter 内部直接 `SaveSettings()`，所以如果卸载时进程还在跑，删掉 `DmtSettings.xml` 后它可能立刻被重写。必须先终止进程（顺序第 1 步）。

---

## I. 本机当前状态（基线，未触发任何副作用）

以下实测确认本机从未启动过 GUI，只跑过 `DMT.exe -v`（`ConsoleMain` 打印版本即退），因此 A、C、D、E 全部为空：

- `HKCU\SOFTWARE\Dual Monitor Tools\DMT` — 不存在
- `HKCU\Software\Classes\directory\Background\shell\GNE_DualMonitorTools` — 不存在
- `HKLM\SOFTWARE\GNE\Dual Monitor Tools` 及 `WOW6432Node` 分支 — 不存在
- `HKCU\...\CurrentVersion\Run` 存在但无 `GNE_DualMonitorTools` 值
- `%APPDATA%\Dual Monitor Tools` — 不存在
- `C:\Windows\System32\DmtWallpaper.scr`、`C:\Windows\SysWOW64\DmtWallpaper.scr` — 不存在
- `C:\Program Files (x86)\Dual Monitor Tools` — 不存在
- `%ProgramData%\...\Start Menu\Programs\Dual Monitor Tools` — 不存在
- `HKCU\Control Panel\Desktop` 的 `SCRNSAVE.EXE` 不存在；`TileWallpaper=0`、`WallpaperStyle=10`（Windows 原始值）

这份基线可用于验证卸载程序是否真的清干净：执行完安装+运行+卸载后，上述项应全部回到"不存在 / Windows 原始值"。
