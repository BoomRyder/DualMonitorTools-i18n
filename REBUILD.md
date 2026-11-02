# Dual Monitor Tools 2.13 — 源码工程说明

本目录由 `DualMonitorTools-2.13-src.zip` 解包重建而成，已在本机完成 **Debug/Release 全量编译 + 单元测试** 验证。

## 1. 这是什么

Dual Monitor Tools（简称 DMT）是 Windows 多显示器辅助工具集，GPLv3 授权，作者 Gerald Evans。
本版本 2.13，程序集版本 `2.13.0.0`。

发布包内的三个可执行程序与本仓库三个工程的对应关系：

| 发布文件 | 工程 | 类型 | 说明 |
|---|---|---|---|
| `DMT.exe` | `DMT/DMT/DMT.csproj` | WinForms 托盘程序 | 主程序，含 Cursor / Launcher / Snap / SwapScreen / WallpaperChanger 五个模块 |
| `DualWallpaper.exe` | `DualWallpaper/DualWallpaper/DualWallpaper.csproj` | WinForms 程序 | 手工为多显示器拼接壁纸 |
| `DmtWallpaper.scr` | `DmtWallpaper/DmtWallpaper/DmtWallpaper.csproj` | 屏保 | 由 PostBuildEvent 把 `DmtWallpaper.exe` 复制成 `.scr` |

## 2. 技术栈判定（依据与证据）

**结论：C# / .NET Framework 4.0 + WinForms，用 Visual Studio 开发，MSBuild（`ToolsVersion 4.0` 旧式 csproj）构建。**

判定依据：

1. 三个 `.sln` 头部均写 `# Visual Studio Express 2012 for Windows Desktop`，`Format Version 12.00`。
2. 四个 `.csproj` 全部是 `xmlns="http://schemas.microsoft.com/developer/msbuild/2003"` 的**非 SDK 风格**项目，`ToolsVersion="4.0"`，由 `Microsoft.CSharp.targets` 编译。
3. 目标框架：`DMT` / `DualWallpaper` / `DmtWallpaper` 为 `v4.0` + `TargetFrameworkProfile=Client`（.NET 4.0 客户端配置文件）；单元测试工程为 `v4.5`。
4. UI 全部为 WinForms：`System.Windows.Forms` 引用、`Form` / `UserControl` 子类型、`.Designer.cs` + `.resx` 成对文件（共 73 个 `.resx`）；`app.manifest` 中声明 Win7/8/8.1/10 兼容与 `dpiAware=True/PM`。
5. 平台互操作通过 P/Invoke：`Library/PInvoke/NativeMethods.cs`、`ActiveDesktop.cs`、`VirtualDesktopManager.cs`；显示器枚举用 `System.Management`（WMI）。
6. 唯一的第三方依赖是单元测试用的 **NUnit 3.7.1**（`DMT/DMT.UnitTests/packages.config`），通过 `packages.config` + `HintPath` 引用，属于旧式 NuGet 集成。
7. 附带的 `development.html` 明确记录构建工具为 *Visual C# 2012 Express Edition*，安装包用 WIX Toolset 3.10 制作（MSI 工程不在源码包内）。

即：**不需要 CMake / Qt / Node，也不是 .NET Core/SDK 风格工程**。用 MSBuild 直接吃 `.sln` 即可。

## 3. 目录结构

```
DualMonitorTools-2.13-src/
├─ DMT/                                 主程序解决方案
│  ├─ DMT.sln                           含 DMT + DMT.UnitTests 两个工程
│  ├─ DMT/
│  │  ├─ Program.cs / ProgramOptions.cs / ConsoleApplication.cs   入口，支持 -v/-x/-? 命令行
│  │  ├─ Controller.cs / AppForm.cs     托盘控制器与主窗体
│  │  ├─ Library/                       可复用基础设施（无 UI 依赖优先）
│  │  │  ├─ PInvoke/                    Win32 API 封装（显示、桌面、虚拟桌面）
│  │  │  ├─ DisplayHandling/            CCD/DisplayConfig 显示器枚举与路径映射
│  │  │  ├─ Environment/                显示器模型、注册表、开机自启、右键菜单
│  │  │  ├─ HotKeys/                    全局热键注册与捕获 UI
│  │  │  ├─ Wallpaper/                  壁纸拼接、适配、切换策略
│  │  │  ├─ WallpaperPlugin/            壁纸源插件契约（IDWC_Plugin / IImageProvider）
│  │  │  ├─ Html/                       轻量 HTTP + HTML 解析（供在线壁纸源抓取）
│  │  │  ├─ Settings/ Transform/ Utils/ Binding/ Command/ Logging/ GuiUtils/ Extensions/
│  │  └─ Modules/                       五个功能模块，各自带 OptionsPanel 与 IModuleService 实现
│  │     ├─ General/  Cursor/  Launcher/  Snap/  SwapScreen/  WallpaperChanger/
│  │     └─ WallpaperChanger/Plugins/   Bing · Flickr · List · LocalDisk · RandomShapes · Unsplash · Url
│  ├─ DMT.UnitTests/                     NUnit 测试（ImageFitter 几何算法，148 个用例）
│  └─ packages/                          NuGet 还原目录
├─ DualWallpaper/  DualWallpaper.sln    壁纸拼接工具
├─ DmtWallpaper/   DmtWallpaper.sln     屏保（输出 .scr）
├─ build.ps1                            本仓新增：一键还原/构建/测试脚本
├─ .gitignore                           本仓新增
├─ tools/nuget.exe                      本仓新增：离线可用的 nuget 命令行
├─ CHANGES.txt  COPYING.txt  README.txt  THANKS.txt
└─ DmtFileLocations-sample.xml
```

## 4. 本机环境要求（已满足）

| 组件 | 要求 | 本机实测 |
|---|---|---|
| Visual Studio | 2019/2022，需「.NET 桌面开发」工作负载 | VS Professional 2019 16.11.60 ✅ |
| MSBuild | 随 VS 提供 | `MSBuild\Current\Bin\MSBuild.exe` ✅ |
| .NET Framework 目标包 | v4.0（含 Client Profile）+ v4.5 | 均有 ✅ |
| .NET Framework 运行时 | 4.0 以上（本机 4.8） | 4.8.09037 ✅ |
| NuGet | 用于还原 NUnit 3.7.1 | `tools/nuget.exe` 6.11.0 ✅ |
| PowerShell | 5.1 或 7.x | 5.1 / 7.x 均可 ✅ |

> 若在**全新机器**上重建，需安装：Visual Studio（勾选 *.NET 桌面开发*，其中包含 .NET Framework 4.0/4.5 目标包与 MSBuild），以及网络可达 nuget.org（或预置 NUnit 包）。

## 5. 构建方式

### 方式 A：脚本（推荐）

```powershell
cd D:\workapsce\common\DMT\DualMonitorTools-2.13-src
.\build.ps1                            # Debug 构建全部三个解决方案
.\build.ps1 -Configuration Release     # Release 构建
.\build.ps1 -Configuration Release -Test   # 构建 + 跑单元测试
.\build.ps1 -Clean                     # 清理 bin/obj
```

### 方式 B：MSBuild 直连

```powershell
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild DMT\DMT.sln                  /p:Configuration=Release
& $msbuild DualWallpaper\DualWallpaper.sln /p:Configuration=Release
& $msbuild DmtWallpaper\DmtWallpaper.sln   /p:Configuration=Release
```

首次构建前需还原 NuGet：

```powershell
.\tools\nuget.exe restore DMT\DMT.sln -PackagesDirectory DMT\packages
```

### 方式 C：Visual Studio IDE

分别打开 `DMT\DMT.sln`、`DualWallpaper\DualWallpaper.sln`、`DmtWallpaper\DmtWallpaper.sln`，F5/F6 即可。
（三个是独立解决方案，互不引用；IDE 会提示升级工具集，选“不升级”保持 v4.0 目标）

### 单测

```powershell
DMT\packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe `
    DMT\DMT.UnitTests\bin\Release\DMT.UnitTests.dll --noresult
```

## 6. 构建产物位置

```
DMT\DMT\bin\Release\DMT.exe
DualWallpaper\DualWallpaper\bin\Release\DualWallpaper.exe
DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.exe
DmtWallpaper\DmtWallpaper\bin\Release\DmtWallpaper.scr     ← PostBuildEvent 生成
```

## 7. 已验证结论

- 三个解决方案 Debug / Release **均 0 error**（仅 CS0649/CS0168/CS0169/CS0108 等历史遗留警告，属原作者代码风格问题，未修改）。
- `DMT.UnitTests` NUnit 3.16.3 运行：**148 / 148 通过**，耗时 ~5.7s。
- 生成的 exe 版本信息读取正常：`DMT.exe 2.13.0.0`、`DualWallpaper.exe 2.13.0.0`、`DmtWallpaper.exe 2.13.0.0`。
- `build.ps1` 在 Windows PowerShell 5.1 与 PowerShell 7 下均可跑通（文件已存为 UTF-8 with BOM 以兼容 5.1 的中文解析）。

## 8. 已知问题（来自源码包本身，非重建引入）

1. `DMT/DMT/Modules/Cursor/CursorModule - Copy.cs`：作者遗留的备份文件，与 `CursorModule.cs` 同类同命名空间，**未被 csproj 收录**，不影响编译。建议删除。
2. `DMT/DMT/Library/HotKeys/HotKeyCaptureForm.{cs,Designer.cs,resx}`：同样**未收录于 csproj**，全仓无任何引用，属废弃代码。
3. `Resources/SwapScreenStrings.resx~`、`Resources/.SwapScreenStrings.resx.un~`：编辑器临时文件。
4. `DMT/DMT.v11.suo`、`DualWallpaper.v11.suo`、`DmtWallpaper.v11.suo`：VS2012 的界面状态缓存，无实际用途（.gitignore 已排除）。
5. 源码包**不含** MSI 安装工程（WIX），如需做安装包需另行搭建。
6. `DMT.csproj` 与 `DmtWallpaper.csproj` 用的是 `TargetFrameworkProfile=Client`（.NET 4.0 客户端配置文件），该 profile 在 VS2019+ 中已不受 UI 支持，但 MSBuild 仍能正常解析编译。

## 9. 未做的事

- 未启动 GUI（`DMT.exe` 无参数会常驻托盘并写入注册表/配置文件，属有副作用操作）。
- 未修改任何原作者代码，未升级目标框架，未做任何“顺手重构”。
