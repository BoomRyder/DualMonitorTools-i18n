# Dual Monitor Tools v2.13（简体中文界面）

Dual Monitor Tools 是一套面向 Windows 多显示器用户的工具集。

原作者：Gerald Evans。以 GNU 通用公共许可证 v3 授权，协议全文见
[`COPYING.txt`](COPYING.txt)。

上游项目主页与使用说明：<http://dualmonitortool.sourceforge.net>

**English documentation: [README.md](README.md)**

## 本中文版改了什么

本仓库是 Dual Monitor Tools 2.13 的**简体中文分支**。在上游 2.13 的基础上只做了两件事：

1. **加入语言包。** 三个程序 `DMT.exe`、`DualWallpaper.exe`、`DmtWallpaper.scr` 的
   全部界面——所有窗口、菜单、对话框、工具提示、提示框、屏保文字——都有简体中文，
   共约 700 条文案。全新安装**默认就是中文**；随时可在
   *选项 → Dual Monitor Tools → 常规 → 界面语言* 里切回 English，
   选择会被记住，下次启动三个程序一起生效。
2. **换了安装方式。** 上游 2.13 用 WiX 打包成 MSI；本分支用 Inno Setup 打包成
   `DualMonitorTools-2.13-zh-Hans-Setup.exe`。新安装包**需要管理员权限**，
   程序图标带 UAC 盾牌（双击即提权），会创建桌面快捷方式，并把屏保装到
   64 位控制面板真正能识别到的位置。

除此之外没有其他改动：没有新增功能，没有改变原有行为，也没有顺手重构上游代码。
上游 2.13 源码包的逐字节快照保存在 git 标签 `upstream-2.13`，
可用来对照本分支改了哪些地方。

## 目录

- [工具](#工具)
- [系统要求](#系统要求)
- [安装](#安装)
- [热键](#热键)
- [魔术字](#魔术字)
- [便携使用](#便携使用)
- [数据文件](#数据文件)
- [卸载](#卸载)
- [已知问题](#已知问题)
- [从源码构建](#从源码构建)
- [致谢](#致谢)
- [变更历史](#变更历史)

## 工具

目前提供 `DMT.exe`、`DualWallpaper.exe` 和 `DmtWallpaper.scr` 三个工具。

### DMT.exe

主程序，常驻 Windows 通知区域（系统托盘），包含以下模块：

- **Cursor（光标）**——控制光标在显示器之间的移动。可以把光标锁定在某台显示器上，
  或设为「粘滞」，从一台显示器移到另一台需要额外的动作。

- **Launcher（启动器）**——用几个按键就能启动常用程序，并把它们摆到任意显示器上
  预设的位置。其他所有 DMT 命令也能用这种方式触发。

- **Snap（快照）**——截取主显示器画面并显示在副显示器上。历史快照可以查看、
  存为文件或复制到剪贴板。

- **Swap Screen（屏幕切换）**——方便地在显示器之间搬移窗口，也能把窗口移到预设位置。

- **Wallpaper Changer（壁纸切换）**——按多种来源周期性地更换壁纸，
  并可控制图片如何在多台显示器上铺开。

### DualWallpaper.exe

手动为多显示器拼接壁纸。

### DmtWallpaper.scr

配合 DMT.exe，在屏保运行时显示你的壁纸。

## 系统要求

Dual Monitor Tools 可运行在任何安装了 .NET 4.0 或更高版本的 Windows 上。

最好有两台或更多显示器，不过部分功能对单显示器用户也有用。

安装包只支持 64 位 Windows。安装前会检查 .NET Framework 4.0 及以上版本，
不满足会用可读的提示中止安装。

## 安装

有两种安装方式：

1. 运行安装包 `DualMonitorTools-2.13-zh-Hans-Setup.exe`——最简单的做法。
2. 从 zip 包便携安装——最灵活，装到便携设备上就用这种方式。

### 用安装包安装

下载 `DualMonitorTools-2.13-zh-Hans-Setup.exe` 直接运行即可。打包方式与安装包具体
写了哪些内容，见 [`installer/README-installer.md`](installer/README-installer.md)。

安装包**需要管理员权限**。它带 UAC 盾牌，双击就会弹出提权提示；如果当前系统关掉了
UAC 因而无法提权，它会拒绝运行并提示你改用「以管理员身份运行」。安装内容：

- `C:\Program Files (x86)\Dual Monitor Tools\`——`DMT.exe`、`DualWallpaper.exe`、
  各自的 config 文件、简体中文资源文件、`COPYING.txt`、本 `README.md`，
  以及一个 `DmtFileLocations.xml`，把数据文件指向 `%APPDATA%\Dual Monitor Tools`。
- `C:\Windows\System32\DmtWallpaper.scr`——屏保。装进 `System32` 而不是 `SysWOW64`，
  这样 64 位控制面板的屏保列表才看得到它。
- 两个开始菜单快捷方式和一个桌面快捷方式。
- 不写 `installed` 注册表标记，因此 DMT 会把自己报告成便携版安装。这是有意的：
  只有这样才能阻止「检查更新」去下载上游英文 MSI 覆盖掉本中文版。

如果安装时 DMT 或 DualWallpaper 正在运行，安装程序会先问你要不要关掉它。选「是」后
先发普通的关闭请求（DMT 会趁机保存魔术字），关不掉才强制结束进程。

### 升级已有安装

直接运行更新的安装包覆盖安装即可。`%APPDATA%\Dual Monitor Tools` 里的数据文件不会被动，
所有设置、魔术字和壁纸源都会保留。

*选项 → Dual Monitor Tools → 常规* 里的「检查更新」仍会查询上游站点并告诉你有没有新版本，
但它只能打开下载页面——程序内自动安装被关掉了，因为那会装成英文 MSI。

### 从上游 MSI 版迁移

上游 MSI 安装与本安装包不能共存：检测到 MSI 安装时安装程序会拒绝继续。迁移步骤：

1. 先确认数据文件的位置。MSI 安装的数据在 `%APPDATA%\Dual Monitor Tools`
   （形如 `C:\Users\<你的用户名>\AppData\Roaming\Dual Monitor Tools`）。
   卸载 MSI **不会**删除这个文件夹，所以数据不会丢；如果该文件夹已经存在，直接跳到第 3 步。
2. 如果你当初是用 zip 装的，数据文件就在 exe 旁边。先把 `DmtMagicWords.xml`、
   `DmtSettings.xml`、`DmtWallpaperProviders.xml` 复制出来，手工建好
   `%APPDATA%\Dual Monitor Tools`，再把它们放进去。
3. 从 Windows「已安装的应用」列表卸载上游 MSI。
4. 运行 `DualMonitorTools-2.13-zh-Hans-Setup.exe`。
5. 从桌面快捷方式启动 DMT，如果之前用过「开机时启动」和「加入桌面右键菜单」，
   到 *选项 → Dual Monitor Tools → 常规* 重新勾上。

### 便携安装

1. 下载 zip 包。
2. 如果旧版本的程序还在跑，先退出。
3. 把 zip 解压到想要的位置。
4. 程序即可运行。由于文件来自网络，Windows 可能会要求你确认允许运行。

## 热键

大部分操作都可以用热键触发。你需要为自己要用的动作定义热键。并非所有组合都可用，
Windows 自己占用了不少，而且每代 Windows 还会占掉更多。

最值得先定义的是 Launcher → 热键 里的「激活魔术字输入」。定义好它之后，
所有其他动作都可以用「这个热键 + 一个魔术字」来完成。英式键盘上 `Win+<OemPipe>`
是个好选择，容易按到。其他键盘布局可能得试几下，看哪些组合既没被占用又容易按。

同一页还有「为当前程序添加魔术字」热键。在别的程序里按下它，会提示你为这个程序创建
一个魔术字，以后就能用它快速启动该程序。`Win+A` 很顺手，但注意 Windows 10 已经占用了
这个组合。

## 魔术字

你可以把程序、网址和 DMT 命令关联到某个魔术字。按下启动器热键后会弹出一个框，
开始输入魔术字即可，带自动补全，通常不用把整个词打完。

同一个魔术字也可以关联多个程序，输入该魔术字时它们会一起被启动。

## 便携使用

这套工具设计上就适合便携使用，可以拷到 U 盘上直接运行。注意要用 zip 包安装，
不要用安装包。

填写文件名时（启动器要启动的程序、壁纸位置），如果目标在 U 盘上，
建议填相对路径以保证便携性。

## 数据文件

便携安装时，工具会把数据文件写在 exe 所在目录，包括：

- `DmtMagicWords.xml`——Launcher 使用的魔术字。
- `DmtWallpaperProviders.xml`——壁纸图片来源。
- `DmtSettings.xml`——其余所有设置。
- 这些文件还会各有一份 `.bak`，是上一版本的备份副本。

用安装包安装时，exe 所在目录会有一个 `DmtFileLocations.xml`，
指定其余 xml 文件的位置，即 `%APPDATA%\Dual Monitor Tools`——与上游 MSI 相同。

### 自定义数据文件位置

只有当你想把数据文件放到 `DMT.exe` 所在目录以外的地方时，才需要
`DmtFileLocations.xml`。

文件名必须是 `DmtFileLocations.xml`，并放在 `DMT.exe` 同目录。这个文件不会被写入，
所以放在 `C:\Program Files (x86)` 下是安全的。

一般只改 `datadirectory` 就够了，除非你要单独控制某个文件名，或者想打开日志文件。
下面的路径里可以用环境变量，也可以写绝对路径。

```xml
<?xml version="1.0" encoding="utf-8"?>

<filelocations>
	<!-- datadirectory 指定下面没有单独指定的所有文件位置的默认目录。
	     必须是完整路径，但可以包含环境变量。
	     运行 DMT 之前要确保该目录已经存在。 -->
	<filelocation name="datadirectory" value="%APPDATA%\Dual Monitor Tools" />

	<!-- 单个文件位置可以写完整路径或相对路径。
	     写相对路径时，相对于 datadirectory（若已指定），
	     否则相对于 DMT.exe 所在目录。 -->

	<!-- 通用设置文件 -->
	<!-- <filelocation name="settings" value="DmtSettings.xml" /> -->

	<!-- 已保存的魔术字 -->
	<!-- <filelocation name="magicwords" value="DmtMagicWords.xml" /> -->

	<!-- 已保存的壁纸源 -->
	<!-- <filelocation name="wallpaperproviders" value="DmtWallpaperProviders.xml" /> -->

	<!-- 生成 Windows 壁纸时使用的图片 -->
	<!-- <filelocation name="wallpaper" value="DmtWallpaper.bmp" /> -->

	<!-- 日志文件（调试用）。不指定则不写日志。 -->
	<!-- <filelocation name="log" value="Dmt.log" /> -->
</filelocations>
```

以上内容存为 `DmtFileLocations.xml`（UTF-8 带 BOM），放在 `DMT.exe` 旁边。

## 卸载

### 用安装包安装后的卸载

从 Windows「已安装的应用」列表卸载即可。卸载会清掉程序文件、屏保、两套快捷方式、
DMT 运行期间可能创建的注册表项（壁纸路径、界面语言、开机自启、桌面右键菜单），
以及数据目录 `%APPDATA%\Dual Monitor Tools`。

**数据目录是无条件删除的，不会询问**——魔术字、设置和壁纸源事后无法找回，
如果可能重装，请先备份 `%APPDATA%\Dual Monitor Tools`。两点注意：

- 只清理执行卸载的那个账号。多用户机器上，其他用户的
  `%APPDATA%\Dual Monitor Tools` 与注册表项会保留。
- 桌面壁纸会被清空（桌面退回纯色），因为 DMT 之前把壁纸指向了数据目录里
  即将被删除的文件。如果你后来换过别的壁纸，那张壁纸不会被改动。

DMT 设置壁纸时写过的 `TileWallpaper` 与 `WallpaperStyle` 这两个 Windows 全局值，
卸载时**刻意不动**。

### 便携安装的卸载

如果曾在 *选项 → Dual Monitor Tools → 常规* 里启用过「开机时启动」或
「加入桌面右键菜单」，删文件之前先把这两项取消勾选。

然后确认没有程序在运行，删除你解压出来的文件和上面提到的数据文件即可。

## 已知问题

如果你希望 DMT 在操作那些「以管理员身份运行」的程序的窗口时完全正常，
那么 DMT 本身也要「以管理员身份运行」。

## 从源码构建

本仓库包含全部源码。构建三个解决方案：

```powershell
cd <仓库根目录>
.\build.ps1                                 # Debug 构建
.\build.ps1 -Configuration Release          # Release 构建
.\build.ps1 -Configuration Release -Test    # 构建 + 跑 363 个 NUnit 用例
.\build.ps1 -Clean                          # 清理 bin/obj
.\build-installer.ps1                       # Release 构建 + 编译安装包
```

需要：Visual Studio 2019/2022 并勾选「.NET 桌面开发」工作负载
（含 .NET Framework 4.0 Client Profile 与 4.5 目标包、MSBuild），
以及可访问 nuget.org（或已预置 `DMT\packages` 下的 NUnit 包）。
编译安装包另外需要 Inno Setup 7。

产物：`DMT\DMT\bin\<配置>\DMT.exe`、
`DualWallpaper\DualWallpaper\bin\<配置>\DualWallpaper.exe`、
`DmtWallpaper\DmtWallpaper\bin\<配置>\DmtWallpaper.exe` 与 `.scr`，
安装包为 `dist\DualMonitorTools-2.13-zh-Hans-Setup.exe`。

简体中文译文放在中性 `resx` 旁边的 `.zh-Hans.resx` 里，另有一组守卫测试保证覆盖率
不缺不漏——本地化约定见 [`AGENTS.md`](AGENTS.md)。安装包的设计与验证方式见
[`installer/README-installer.md`](installer/README-installer.md)。

## 致谢

特别感谢：

- Shawn Steele：为 DualWallpaper 增加背景色选择。
- Nithin Philips：为 DualWallpaper 增加拖放支持。
- Christopher Drexler：为 DualWallpaper 增加命令行选项。
- Ryan Kirby：指出上一版屏幕热键的缺陷。
- Mario D'Amico：增加 Bing 壁纸下载器。

以及所有在论坛上反馈缺陷、提出改进建议的人。

## 变更历史

本中文分支相对上游 2.13 的改动，已在本文开头「[本中文版改了什么](#本中文版改了什么)」
一节列出，概要为：全量简体中文化 + 新增界面语言选项 + 用 Inno Setup 重做安装包
（顺带修掉上游两处安装缺陷）。

上游 2.13 及之前各版本（1.0 至 2.13，共 60 个版本）的变更记录属于上游历史，
未逐条翻译，见英文版 [`README.md` 的 Change History 一节](README.md#change-history)。
