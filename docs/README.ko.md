# Dual Monitor Tools

**Windows 다중 모니터용 커서, 창, 실행기, 배경 화면 도구.**

[English](../README.md) | [简体中文](./README.zh-CN.md) | [繁體中文](./README.zh-Hant.md) | [日本語](./README.ja.md) | **한국어**

[기능](#기능) • [시작](#시작) • [빌드](#빌드) • [변경 기록](#변경-기록) • [라이선스](#라이선스)

---

Dual Monitor Tools는 Gerald Evans의 Windows 다중 모니터 도구 모음입니다. 이 저장소는 2.13의 다국어 분기입니다. 인터페이스는 간체 중국어, 번체 중국어, 일본어, 한국어, 영어 중에서 고를 수 있습니다. 처음 실행은 간체 중국어입니다.

업스트림 프로젝트: <http://dualmonitortool.sourceforge.net>

## 기능

- 🖱️ **커서** — 포인터를 모니터 하나에 고정하거나, 다음 화면으로 넘어가려면 한 번 더 움직여야 하는 스티키로 설정
- 🚀 **실행기** — 짧은 매직 워드로 프로그램, 문서, 웹 페이지를 열고 창을 정한 위치에 둠
- 📸 **스냅** — 주 모니터 화면을 다른 화면에 보여주고, 나중에 보거나 저장하거나 복사
- ↔️ **화면 전환** — 창을 모니터 사이로 옮기거나 절반, 사분의 일, 저장한 영역에 맞춤
- 🖼️ **배경 화면 변경** — 여러 출처에서 배경 화면을 바꾸고, 이미지 하나가 여러 모니터에 어떻게 깔릴지 정함
- 🧩 **DualWallpaper** — 다중 모니터 배경 화면을 직접 맞춤
- 💤 **DmtWallpaper** — DMT가 마지막으로 만든 배경 화면을 보여주는 화면 보호기
- 🌐 **다섯 가지 인터페이스 언어** — 간체 중국어, 번체 중국어, 일본어, 한국어, 영어. *옵션 → Dual Monitor Tools → 일반 → 언어*에서 바꾸며, 선택은 기억됨

## 시작

| 형태 | 실행 | 설명 |
| --- | --- | --- |
| **설치 프로그램** | `installer\DualMonitorTools.msi` | 관리자 권한 필요. 데이터는 `%APPDATA%\Dual Monitor Tools` |
| **휴대용** | 원하는 폴더의 `DMT.exe` | 데이터는 exe 옆. `DmtFileLocations.xml`은 같이 두지 않음 |
| **소스** | `.\build.ps1` 다음 `installer\Build.bat` | [빌드](#빌드) 참고 |

설치 프로그램은 업스트림 WiX MSI입니다. `installed=1`을 쓰고, 화면 보호기는 32비트 시스템 폴더로 들어갑니다(64비트 Windows에서는 `SysWOW64`). 비어 있는 바탕 화면 바로 가기 메뉴 키도 남습니다. 언어 위성 어셈블리는 포함되지 않습니다. dll을 따로 두지 않으면 화면은 영어로 돌아갑니다.

「업데이트 확인」은 업스트림 버전을 열거나 설치하기 전에 묻습니다. 기본값은 아니요입니다. 예를 고르면 이 빌드가 업스트림 영어 원본으로 바뀝니다.

## 빌드

Visual Studio 2019 또는 2022의 .NET 데스크톱 개발 워크로드(.NET Framework 4.0 Client Profile과 4.5)와, MSI용 WiX Toolset 3.14가 필요합니다.

```powershell
.\build.ps1
.\build.ps1 -Configuration Release -Test
cd installer
.\Build.bat
```

언어 위성은 각 프로그램 옆의 `zh-Hans`, `zh-Hant`, `ja`, `ko` 폴더에 만들어집니다.

## 변경 기록

이 분기의 변경은 다섯 언어 인터페이스와, 업데이트 확인이 업스트림 다운로드를 따르기 전에 확인하는 것입니다. 설치 프로그램은 다시 원작자의 WiX 프로젝트입니다.

Gerald Evans의 1.0부터 2.13까지 기록은 [CHANGELOG.md](./CHANGELOG.md)에 있습니다. MSI에 들어가는 짧은 기록은 저장소 루트의 `CHANGES.txt`입니다.

## 라이선스

[GNU General Public License v3](../COPYING.txt). 원 프로그램의 저작권은 Gerald Evans에게 있습니다. 이 라이선스에 따라 복제, 수정, 배포할 수 있습니다. 보증은 없습니다.
