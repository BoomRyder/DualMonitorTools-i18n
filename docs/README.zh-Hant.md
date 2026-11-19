# Dual Monitor Tools

**給 Windows 多螢幕使用的游標、視窗、啟動器與桌布工具。**

[English](../README.md) | [简体中文](./README.zh-CN.md) | **繁體中文** | [日本語](./README.ja.md) | [한국어](./README.ko.md)

[功能](#功能) • [開始使用](#開始使用) • [建置](#建置) • [更新紀錄](#更新紀錄) • [授權](#授權)

---

Dual Monitor Tools 是 Gerald Evans 的 Windows 多螢幕工具集。這個倉庫是 2.13 的多語言分支。介面可以是簡體中文、繁體中文、日文、韓文或英文。第一次啟動是簡體中文。

上游專案：<http://dualmonitortool.sourceforge.net>

## 功能

- 🖱️ **游標** — 把指標鎖在一台螢幕上，或設成黏著，移到下一台要多費一點力氣
- 🚀 **啟動器** — 用一個魔術字開啟程式、文件和網頁，並把視窗放到指定位置
- 📸 **快照** — 把主螢幕畫面顯示到另一台螢幕，之後可以檢視、儲存或複製
- ↔️ **交換畫面** — 在螢幕之間移動視窗，或吸附到半螢幕、四分之一螢幕和已儲存的區域
- 🖼️ **桌布更換** — 依多種來源輪換桌布，並控制一張圖如何鋪到多台螢幕
- 🧩 **DualWallpaper** — 手動拼接多螢幕桌布
- 💤 **DmtWallpaper** — 螢幕保護程式，顯示 DMT 最近產生的桌布
- 🌐 **五種介面語言** — 簡體中文、繁體中文、日文、韓文、英文。在 *選項 → Dual Monitor Tools → 一般 → 語言* 切換，選擇會被記住

## 開始使用

| 方式 | 執行什麼 | 說明 |
| --- | --- | --- |
| **安裝包** | `installer\DualMonitorTools.msi` | 需要系統管理員。資料在 `%APPDATA%\Dual Monitor Tools` |
| **可攜** | 任意資料夾裡的 `DMT.exe` | 資料寫在 exe 旁邊。不要帶上 `DmtFileLocations.xml` |
| **原始碼** | `.\build.ps1`，然後 `installer\Build.bat` | 見 [建置](#建置) |

安裝包是上游的 WiX MSI。它會寫入 `installed=1`，螢幕保護程式經過 32 位元系統目錄（64 位元 Windows 上是 `SysWOW64`），並仍會留下空白的桌面右鍵鍵。套件不含語言衛星組件；不另外放上這些 dll 時，介面回到英文。

「檢查更新」在開啟或安裝上游版本之前會先詢問。預設是「否」。選「是」會換成上游的英文原版。

## 建置

需要 Visual Studio 2019 或 2022 的 .NET 桌面開發工作負載（.NET Framework 4.0 Client Profile 與 4.5），以及 WiX Toolset 3.14。

```powershell
.\build.ps1
.\build.ps1 -Configuration Release -Test
cd installer
.\Build.bat
```

語言衛星在各程式旁邊的 `zh-Hans`、`zh-Hant`、`ja`、`ko` 資料夾。

## 更新紀錄

這個分支的改動是五種介面語言，以及檢查更新在跟隨上游下載之前先確認。安裝包重新使用原作者的 WiX 工程。

Gerald Evans 從 1.0 到 2.13 的更新紀錄在 [CHANGELOG.md](./CHANGELOG.md)。安裝包裡附帶的短紀錄仍是倉庫根目錄的 `CHANGES.txt`。

## 授權

[GNU 通用公眾授權條款第 3 版](../COPYING.txt)。原程式版權歸 Gerald Evans。依該授權可以複製、修改和散佈。程式不提供擔保。
