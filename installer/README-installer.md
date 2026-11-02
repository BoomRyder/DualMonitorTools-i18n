# 安装包（Inno Setup）

本目录是 Dual Monitor Tools 2.13 简体中文版的 Windows 安装工程。产物是
`dist\DualMonitorTools-2.13-zh-Hans-Setup.exe`，用 **Inno Setup 7** 编译。

原版发布物是 WiX 生成的 MSI，源码包里不含它的安装工程；本项目重新用 Inno
Setup 实现，行为对齐原 MSI，并修掉了原 MSI 的两处缺陷（见下）。

设计文档：`docs/superpowers/specs/2026-11-02-dmt-inno-installer-design.md`

## 构建

```powershell
.\build-installer.ps1                 # 先构建 Release 产物，再编译安装包
.\build-installer.ps1 -SkipBuild      # 复用已有的 bin\Release
```

脚本用下列顺序定位 `ISCC.exe`：

1. 注册表 `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1`
   的 `InstallLocation`（以及 `WOW6432Node` 视图）
2. 环境变量 `INNO_SETUP_DIR`
3. `D:\Program Files\Inno Setup 7`、`C:\Program Files (x86)\Inno Setup 7`、
   `C:\Program Files\Inno Setup 7`

都找不到会直接报错并说明该装什么。

也可以直接用 Inno 的 IDE（`ISIDE.exe`）打开 `installer\DualMonitorTools.iss`；
此时 `RepoRoot` 回退为脚本所在目录的上一级。

## 安装包写入的内容

### 程序目录 `C:\Program Files (x86)\Dual Monitor Tools\`

`DMT.exe`、`DMT.exe.config`、`DualWallpaper.exe`（+ `.config`）、
`DmtWallpaper.exe`（+ `.config`）、三个 `zh-Hans\*.resources.dll`、
`DmtFileLocations.xml`、`COPYING.txt`、`README.md`。

`zh-Hans\*.resources.dll` 是简体中文界面**必需**件，必须落在 `zh-Hans` 子目录，
放到程序根目录不会生效。

### 系统目录 `C:\Windows\System32\DmtWallpaper.scr`

用 `{sysnative}` 而不是 `{sys}`：32 位安装包写 `{sys}` 会被 WOW64 重定向到
`SysWOW64`，而 `desk.cpl` 是 64 位进程，看不到那里的屏保。这正是原 MSI 的缺陷。

### 开始菜单 `Dual Monitor Tools\`

两个快捷方式：`Dual Monitor Tools` → `DMT.exe`，`DualWallpaper` → `DualWallpaper.exe`。

### 注册表

| 位置 | 内容 | 卸载时 |
|---|---|---|
| `HKLM\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools` | `ScreenSaverInstalled` = DWORD 1 | 删键 |
| `HKCU\Software\Classes\directory\Background\shell\GNE_DualMonitorTools` | 空键 | 删整棵子树 |

**故意不写** `HKLM\...\GNE\Dual Monitor Tools\installed`。写了它
`GeneralModule.IsMsiInstall` 就返回真，程序的"检查更新"会显示"安装新版本"，
该按钮会下载并运行**英文原版 MSI**，覆盖中文版。

## 卸载清理

Inno 自己删掉的东西之外，`[Code]` 段的 `CurUninstallStepChanged`
（`usPostUninstall`）还会做这几件事，每步独立 try，单步失败不影响后续：

1. 若 `HKCU\Control Panel\Desktop\Wallpaper` 指向 `%APPDATA%\Dual Monitor Tools\`，
   把它清空并调用 `SystemParametersInfo(SPI_SETDESKWALLPAPER)`，桌面回到纯色。
   **指向别处时不动**——那是用户自己的壁纸。
2. 删 `HKCU\SOFTWARE\Dual Monitor Tools` 整棵子树（`Language`、`DmtWallpaperPath`）
3. 删 `HKCU\...\Run` 的 `GNE_DualMonitorTools` 值
4. 删 `HKCU\Software\Classes\directory\Background\shell\GNE_DualMonitorTools` 整棵子树
   （右键菜单开关打开过就会生成 11 条子命令，只删空键会全部残留）
5. 删空掉的 `HKLM\SOFTWARE\WOW6432Node\GNE`
6. 删 `%APPDATA%\Dual Monitor Tools\` 整个目录（用户选择"数据全删"）
7. 删 `%TEMP%\DualMonitorTools.msi`（"检查更新"的下载残留）

不删 `HKCU\Control Panel\Desktop` 的 `TileWallpaper` / `WallpaperStyle`：那是 Windows
全局设置，用户自己也可能改过。

## 与原 MSI 的差异

1. **设置面板显示"便携版安装"**。因为不写 `installed` 标记，尽管它确实是安装版。
   这是关掉自动更新的必然代价；改文案需要动作者源码，本项目不做。
2. **没有"修复（Repair）"功能**。Inno 没有 MSI 的修复；文件损坏只能卸载重装。
3. **不能与原 MSI 共存**。检测到原版（卸载项 `{59CFB0C1-…}` 或
   `GNE\Dual Monitor Tools\installed`，两个注册表视图都查）直接拒绝安装，
   要求先去"程序和功能"卸载。
4. **屏保装到 `System32`** 而非原版的 `SysWOW64`（修正，见上）。
5. **文档只装 `README.md` + `COPYING.txt`**。原 MSI 装 `CHANGES.txt` / `README.txt` /
   `THANKS.txt`，本仓库已把它们合并进 `README.md`。
6. **装完启动用 `runasoriginaluser`**。原 MSI 的 `StartAppOnExit` 在提权上下文里
   启动 `dmt.exe`，程序以管理员身份跑；这里以原用户身份启动。

## 已知警告

编译时 ISCC 会报一条 `UsedUserAreasWarning`：`PrivilegesRequired=admin` 却写
`HKCU`。这是有意为之——快捷方式和右键菜单是 per-user 的，而程序目录和
`System32` 需要管理员权限。单用户桌面环境下 `HKCU` 与提权用户是同一个人，
行为正确；多用户共用一台机器时，右键菜单只会出现在安装者账号下。

## 验证

### 静态断言（无需管理员，随 `build.ps1 -Test` 跑）

`DMT\DMT.UnitTests\Installer\InstallerAssets_Test.cs` 检查：数据目录重定向文件
存在且是 UTF-8 BOM、恰好一条 `datadirectory`；`.iss` 的关键 `[Setup]` 指令、
产品名与版本、`[CustomMessages]` 中英成对、`DestDir {sysnative}`、六条卸载步骤、
每个 Release 产物都确实被 `[Files]` 收录、**且没有写 `installed` 标记**。

### 行为验证（需要管理员权限，会真实改注册表与系统目录）

```powershell
.\build-installer.ps1
.\installer\tests\Test-Preflight.ps1 -SetupExe .\dist\DualMonitorTools-2.13-zh-Hans-Setup.exe
.\installer\tests\Test-Uninstall.ps1  -SetupExe .\dist\DualMonitorTools-2.13-zh-Hans-Setup.exe
```

- `Test-Preflight.ps1`：伪造原版 MSI 的 `installed` 标记 → 静默安装必须被拒
  （退出码非 0、零文件落地）→ 复原注册表。
- `Test-Uninstall.ps1`：阶段一装到临时目录 → 制造全部运行时残留 → 静默卸载 →
  逐项断言消失；阶段二验证"用户自己的壁纸不会被误清"。脚本会先保存
  `HKCU\Control Panel\Desktop` 的 `Wallpaper` / `TileWallpaper` / `WallpaperStyle`
  并在结束时复原，包括重新应用壁纸。

两个脚本都要求跑之前基线是干净的（相关键与目录都不存在），否则会直接报
"基线已被污染"并中止，不会带着脏状态往下跑。
