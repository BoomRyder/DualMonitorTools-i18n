# 安装包（Inno Setup）

本目录是 Dual Monitor Tools 2.13 简体中文版的 Windows 安装工程。产物是
`dist\DualMonitorTools-2.13-zh-Hans-Setup.exe`，用 **Inno Setup 7** 编译。

原版发布物是 WiX 生成的 MSI，源码包里不含它的安装工程；本项目重新用 Inno
Setup 实现，行为对齐原 MSI，并修掉了原 MSI 的两处缺陷（见下）。

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
`DmtFileLocations.xml`、`COPYING.txt`、`README.md`、`README.zh-CN.md`。

`zh-Hans\*.resources.dll` 是简体中文界面**必需**件，必须落在 `zh-Hans` 子目录，
放到程序根目录不会生效。

### 系统目录 `C:\Windows\System32\DmtWallpaper.scr`

用 `{sysnative}` 而不是 `{sys}`：32 位安装包写 `{sys}` 会被 WOW64 重定向到
`SysWOW64`，而 `desk.cpl` 是 64 位进程，看不到那里的屏保。这正是原 MSI 的缺陷。

### 快捷方式

开始菜单 `Dual Monitor Tools\`：`Dual Monitor Tools` → `DMT.exe`，`DualWallpaper` →
`DualWallpaper.exe`。

桌面：`Dual Monitor Tools` → `DMT.exe`。admin 模式下 `{autodesktop}` 是公共桌面
（`C:\Users\Public\Desktop`），卸载时一并删除。原 MSI 不建桌面图标。

### 首次启动的界面语言

安装完成、第一次运行 `DMT.exe` 时界面是**简体中文**。语言偏好存在
`HKCU\SOFTWARE\Dual Monitor Tools\DMT\Language`，没有该值时默认 `zh-Hans`；在
Options → General → 语言 里可以切回 English，选择会被记住（切换后需重启程序生效）。

### 注册表

| 位置 | 内容 | 卸载时 |
|---|---|---|
| `HKLM\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools` | `ScreenSaverInstalled` = DWORD 1 | 删键 |
| `HKCU\Software\Classes\directory\Background\shell\GNE_DualMonitorTools` | **不创建**（见下） | 删整棵子树 |

**故意不建那个空的右键菜单键。** 原 MSI 在 `HKCU\...\directory\Background\shell` 下建了一个
空占位键，Explorer 会把裸键名 `GNE_DualMonitorTools` 当成一个菜单项显示出来，点下去报
「该文件没有与之关联的应用来执行该操作」——因为它没有 `command` 子键。真正的菜单树由
DMT 自己在用户勾选「桌面右键菜单」时创建（`DesktopContextMenu.Add`，11 条子命令）。
安装程序在 `CurStepChanged(ssPostInstall)` 里还会顺手删掉**旧安装留下的空键**（存在但
没有 `shell` 子键），这样重装一次就能修好右键菜单已经坏掉的机器。

**故意不写** `HKLM\...\GNE\Dual Monitor Tools\installed`。写了它
`GeneralModule.IsMsiInstall` 就返回真，程序的"检查更新"会显示"安装新版本"，
该按钮会下载并运行**英文原版 MSI**，覆盖中文版。

## 卸载清理

Inno 自己删掉的东西之外，`[Code]` 段的 `CurUninstallStepChanged`
（`usPostUninstall`）还会做这几件事。这些函数返回 Boolean 而不抛异常，
所以每一步都检查返回值并在日志里区分成功与失败（`/LOG` 可查）：

1. 若 `HKCU\Control Panel\Desktop\Wallpaper` 指向 `%APPDATA%\Dual Monitor Tools\`，
   把它清空并用 `SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, '', SPIF_UPDATEINIFILE or SPIF_SENDCHANGE)`
   让桌面立刻重绘成纯色。**指向别处时不动**——那是用户自己的壁纸。
2. 删 `HKCU\SOFTWARE\Dual Monitor Tools` 整棵子树（`Language`、`DmtWallpaperPath`）
3. 删 `HKCU\...\Run` 的 `GNE_DualMonitorTools` 值
4. 删 `HKCU\Software\Classes\directory\Background\shell\GNE_DualMonitorTools` 整棵子树
   （右键菜单开关打开过就会生成 11 条子命令，只删空键会全部残留）
5. 删空掉的 `HKLM\SOFTWARE\WOW6432Node\GNE`（两个注册表视图都试）
6. 若 `HKCU\Control Panel\Desktop\SCRNSAVE.EXE` 指向 `DmtWallpaper.scr`，删掉这个值
   ——屏保文件已经被卸掉，留着就是悬空引用
7. 删 `%APPDATA%\Dual Monitor Tools\` 整个目录（**无条件删除**，见下面的差异第 7 条）
8. 删 `%TEMP%\DualMonitorTools.msi`（"检查更新"的下载残留）

不删 `HKCU\Control Panel\Desktop` 的 `TileWallpaper` / `WallpaperStyle`：那是 Windows
全局设置，用户自己也可能改过；安装前的原值没有被记录，删掉比留着更糟。卸载后
若桌面图标文字背景或壁纸拟合方式不对，到「个性化 → 背景」重新选一次即可。

## 与原 MSI 的差异

1. **设置面板显示"便携版安装"**。因为不写 `installed` 标记，尽管它确实是安装版。
   这是关掉自动更新的必然代价；改文案需要动作者源码，本项目不做。
2. **多了桌面快捷方式，且不建空白的右键菜单键**。原 MSI 只有开始菜单图标，还在
   `HKCU\...\directory\Background\shell` 下留了一个空占位键——Explorer 会把裸键名
   `GNE_DualMonitorTools` 显示成一个菜单项，点下去报"没有与之关联的应用"。
   本安装包建桌面图标，并且不再创建这个空键（已有空键在安装时清掉）。
3. **没有"修复（Repair）"功能**。Inno 没有 MSI 的修复；文件损坏只能卸载重装。
4. **不能与原 MSI 共存**。检测到原版（卸载项 `{59CFB0C1-…}` 或
   `GNE\Dual Monitor Tools` 下的 `installed` **值**）直接拒绝安装，要求先去
   "程序和功能"卸载。注意判定只认 `installed` 值：本安装包自己也往同一个键写
   `ScreenSaverInstalled`，若用"键存在"判定，装过一次后就再也装不了第二次。
5. **屏保装到 `System32`** 而非原版的 `SysWOW64`（修正，见上）。
6. **文档装 `README.md` + `README.zh-CN.md` + `COPYING.txt`**。原 MSI 装 `CHANGES.txt` / `README.txt` /
   `THANKS.txt`，本仓库已把它们合并进 `README.md`。
7. **装完启动用 `runasoriginaluser`**。原 MSI 的 `StartAppOnExit` 在提权上下文里
   启动 `dmt.exe`，程序以管理员身份跑；这里以原用户身份启动。
8. **卸载时无条件删除用户数据目录**（`%APPDATA%\Dual Monitor Tools\`，含设置、热键、
   魔术字、壁纸源配置），不弹窗征询，静默卸载同样如此。这是本项目的既定选择：
   卸载即彻底清除，不留残余。**误删无法恢复**，需要保留设置的用户请在卸载前自行备份。
9. **多用户机器上卸载只清当前账号**。`{userappdata}` 与三处 HKCU 清理都作用于
   执行卸载的那个账号；其他用过 DMT 的账号，其 `%APPDATA%\Dual Monitor Tools\`
   与右键菜单子树会原样留下，需各自登录后手工删除。

## 管理员权限

安装包**必须**以管理员身份运行：要写 `C:\Program Files (x86)`、`System32` 和 `HKLM`。

Inno 编出来的 `Setup.exe` 清单一律是 `asInvoker`（loader 运行期用 `runas` 自重启提权），
所以资源管理器**不会**画 UAC 盾牌，双击也看不出它是需要管理员的。要做到"带盾牌、
双击直接以管理员模式运行"，就得让 exe 自己的清单是 `requireAdministrator`。两条
想当然的路都不行：

- **不能用 `mt.exe` 一类工具重写清单**：它按 PE 镜像大小重写整个文件，2338213 字节
  的包被截断成 849920（正好是镜像大小），Inno 追加在 exe 后面的安装数据包全丢，包
  直接损坏。
- **`UseSetupLdr=no` 不解决问题**：产出的单文件 exe 清单仍是 `asInvoker`。

可用的是**等长原位替换**，已固化在 `build-installer.ps1` 的
`Set-SetupRequiresAdministrator`：把清单里的 `level="asInvoker"`（17 字节）换成
`level="requireAdministrator"`（28 字节），多出的 11 字节从 `<trustInfo>` 前那段
Inno 自带的空白行（278 个空格）里砍掉。于是文件总长与所有偏移不变，追加的安装数据
包完好，而 Windows 读到的清单已经是 `requireAdministrator`——盾牌、UAC 提示、双击
即管理员都随之生效。脚本每次编译后自动执行并校验替换前后长度一致；找不到
`asInvoker` 或空白不够时直接报错，不会悄悄产出没有盾牌的包。

`[Setup] PrivilegesRequired=admin` 仍然保留作为兜底（用户从命令行、计划任务等
不经过资源管理器的路径启动时），另有两条闸门把失败变成可读的诊断：

- `InitializeSetup` 里 `if not IsAdmin` 就弹 `ErrNeedAdmin` 并退出，不会走到建目录。
- `PrepareToInstall` 用 `DirectoryIsWritable()` 真写一个临时文件再删掉来探测
  `{app}`。只用 `ForceDirectories()` 不够——杀软"文件夹保护"会放过建目录、拦住写
  文件。探测失败时返回 `ErrCannotWrite`，消息里带完整路径，并提示杀软或用 `/LOG`
  看日志，而不是丢一句 Inno 的 "unable to create the directory"。

若用户报告"无法创建目录"：先确认是不是右键"以管理员身份运行"解决的（那就是没提
权）；若已是管理员仍失败，通常是杀毒软件（本机装的是 360，此前已两次干扰过自动化）
拦了对 `Program Files` 的写入，需要在杀软里放行该安装包。

## 检测到 DMT 正在运行时

`InitializeSetup` 用 `FindWindowByWindowName('DMT_GUI_WINDOW')` 判断 DMT 是否在跑。
真在跑时弹 `ErrDmtRunning` 询问「现在就关闭吗？」（默认「是」），确认后：

1. 先执行 `taskkill /IM DMT.exe`（**不带** `/F`）——发的是 `WM_CLOSE`，DMT 收到后
   正常退出并保存魔术字，最近的修改不会丢；
2. 5 秒内窗口没消失，才 `taskkill /F /IM DMT.exe` 强杀；
3. 仍关不掉（被别的账号或沙箱占着）就弹 `ErrDmtNotClosed` 中止安装，绝不带着运行中
   的进程去覆盖文件。

静默安装（`/VERYSILENT`）下这个询问取默认值「是」，所以也会自动关掉 DMT。
`CloseApplications=yes` 保留着，作为 `{app}` 里其它被占用文件的兜底。

## 已知警告

编译时 ISCC 会报一条 `UsedUserAreasWarning`：`PrivilegesRequired=admin` 却写
`HKCU`。这是有意为之——快捷方式和右键菜单是 per-user 的，而程序目录和
`System32` 需要管理员权限。单用户桌面环境下 `HKCU` 与提权用户是同一个人，
行为正确；多用户共用一台机器时，右键菜单只会出现在安装者账号下。

## 验证

### 静态断言（无需管理员，随 `build.ps1 -Test` 跑）

`DMT\DMT.UnitTests\Installer\InstallerAssets_Test.cs` 检查：数据目录重定向文件
存在且是 UTF-8 BOM、恰好一条 `datadirectory`；`.iss` 的关键 `[Setup]` 指令、
产品名与版本、`[CustomMessages]` 中英成对、`DestDir {sysnative}`、八个卸载步骤、
`DmtFileLocations.xml`/`COPYING.txt`/`README.md`/`README.zh-CN.md` 都被收录、三个卫星程序集都落在
`{app}\zh-Hans`、**没有写 `installed` 标记**、**原版 MSI 的判定查的是 `installed`
值而不是键存在性**、**非管理员时拒绝安装**、**装文件前先探测目录可写**。

### 行为验证（需要管理员权限，会真实改注册表与系统目录）

自动化行为脚本未随仓库发布（会真实写 `HKCU` 与 `System32`，不适合留在源码树里）。
需要复核时手工按下面几点验证，每步做完都要把基线复原：

1. **前置检查**：伪造 `HKLM\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools\installed = 1`，
   静默运行安装包（`/VERYSILENT /SUPPRESSMSGBOXES`）必须退出码非 0 且零文件落地；
   删掉伪造值后再装必须成功。这一步同时验证"二次安装不会被误判成原版 MSI 还在"。
2. **安装落地**：程序目录、`System32\DmtWallpaper.scr`（**不是** `SysWOW64`）、开始菜单组、
   公共桌面快捷方式全部存在；`HKCU\...\directory\Background\shell\GNE_DualMonitorTools`
   **不存在**。
3. **卸载清理**：制造运行时残留（`HKCU\SOFTWARE\Dual Monitor Tools`、
   `Run\GNE_DualMonitorTools`、右键菜单子树、`SCRNSAVE.EXE` 指向本产品 `.scr`、
   `%APPDATA%\Dual Monitor Tools`）后静默卸载，逐项断言消失。
4. **壁纸**：把 `HKCU\Control Panel\Desktop\Wallpaper` 指向数据目录内的壁纸 → 卸载后应被清空；
   指向别处的用户壁纸 → 卸载后**必须原样保留**。

两个注意点：用 `ProcessStartInfo`（`UseShellExecute=$false`）启动安装包，不要用
`Start-Process`，否则 MOTW / 杀软检查弹出"打开文件 - 安全警告"对话框会让静默安装一直等下去；
Inno 的卸载器会把自己复制到 `%TEMP%` 再重启，第一个进程退出不等于卸载完成，断言前要轮询等
`[Code]` 清理跑完。运行前基线必须干净（相关键、目录、屏保、开始菜单组、卸载项都不存在）。

