# Dual Monitor Tools

**面向 Windows 多显示器的光标、窗口、启动器和壁纸工具。**

[English](../README.md) | **简体中文** | [繁體中文](./README.zh-Hant.md) | [日本語](./README.ja.md) | [한국어](./README.ko.md)

[功能](#功能) • [开始使用](#开始使用) • [构建](#构建) • [更新日志](#更新日志) • [许可证](#许可证)

---

Dual Monitor Tools 是 Gerald Evans 的 Windows 多显示器工具集。本仓库是 2.13 的多语言分支。界面可以是简体中文、繁体中文、日文、韩文或英文。第一次启动是简体中文。

上游项目：<http://dualmonitortool.sourceforge.net>

## 功能

- 🖱️ **光标** — 把指针锁在一台显示器上，或设成粘滞，跨到下一台需要多费一点力气
- 🚀 **启动器** — 用一个魔术字打开程序、文档和网页，并把窗口放到指定位置
- 📸 **快照** — 把主显示器画面显示到另一台屏幕，之后可以查看、保存或复制
- ↔️ **交换屏幕** — 在显示器之间移动窗口，或吸附到半屏、四分之一屏和保存的区域
- 🖼️ **壁纸切换** — 按多种来源轮换壁纸，并控制一张图怎样铺到多台显示器
- 🧩 **DualWallpaper** — 手工拼接多显示器壁纸
- 💤 **DmtWallpaper** — 屏保，显示 DMT 最近生成的壁纸
- 🌐 **五种界面语言** — 简体中文、繁体中文、日文、韩文、英文。在 *选项 → Dual Monitor Tools → 常规 → 语言* 里切换，选择会被记住

## 开始使用

| 方式 | 运行什么 | 说明 |
| --- | --- | --- |
| **安装包** | `installer\DualMonitorTools.msi` | 需要管理员。数据在 `%APPDATA%\Dual Monitor Tools` |
| **便携** | 任意目录里的 `DMT.exe` | 数据写在 exe 旁边。不要带上 `DmtFileLocations.xml` |
| **源码** | `.\build.ps1`，然后 `installer\Build.bat` | 见 [构建](#构建) |

安装包是上游的 WiX MSI。它会写 `installed=1`，屏保经过 32 位系统目录（64 位 Windows 上是 `SysWOW64`），并仍会留下空的桌面右键键。包里没有语言卫星程序集；不另放这些 dll 时，界面回到英文。

「检查更新」在打开或安装上游版本之前会先问。默认是「否」。选「是」会换成上游的英文原版。

## 构建

需要 Visual Studio 2019 或 2022 的 .NET 桌面开发工作负载（.NET Framework 4.0 Client Profile 和 4.5），以及 WiX Toolset 3.14。

```powershell
.\build.ps1
.\build.ps1 -Configuration Release -Test
cd installer
.\Build.bat
```

语言卫星在各程序旁边的 `zh-Hans`、`zh-Hant`、`ja`、`ko` 目录。

## 更新日志

本分支的改动是五种界面语言，以及检查更新在跟随上游下载之前先确认。安装包重新使用原作者的 WiX 工程。

Gerald Evans 从 1.0 到 2.13 的更新记录在 [CHANGELOG.md](./CHANGELOG.md)。安装包里附带的短记录仍是仓库根目录的 `CHANGES.txt`。

## 许可证

[GNU 通用公共许可证第 3 版](../COPYING.txt)。原程序版权归 Gerald Evans。按该许可证可以复制、修改和分发。程序不提供担保。
