# DMT 简体中文 i18n 与语言选择 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 Dual Monitor Tools 2.13 的三个可执行程序增加运行期界面语言选择（Options 树里的 Language 节点），并提供一份覆盖全部可见文案的简体中文翻译。

**Architecture:** 语言偏好存 `HKCU\SOFTWARE\Dual Monitor Tools\DMT` 的 `Language` 值（三个程序共用的最小通道）。显示层用 .NET Framework 自带的标准 ResX 卫星程序集：中性 resx 存英文，`zh-Hans` 卫星 resx 只放已翻译键，缺键由 CLR 自动回退英文。启动时 `UiCulture.Apply` 显式把 `CurrentUICulture` 与每个资源类的静态 `Culture` 属性设成选定语言，保证任意线程读到的语言一致。

**Tech Stack:** C# 5（不可用 C# 6+ 语法）、.NET Framework 4.0 + Client Profile（UnitTests 为 4.5）、WinForms、MSBuild 旧式 csproj、NUnit 3.7.1、ResX 卫星程序集（MSBuild 自动调用 `al.exe`）。

**Spec:** `docs/superpowers/specs/2026-11-02-dmt-zh-hans-i18n-design.md`

## Global Constraints

以下为 spec 的项目级要求，逐条照搬，每个任务隐式包含本节：

- **目标框架不得改动**：`DMT` / `DualWallpaper` / `DmtWallpaper` 保持 `v4.0` + `TargetFrameworkProfile=Client`；`DMT.UnitTests` 保持 `v4.5`。
- **不使用 C# 6+ 语法**：禁 `?.`、`nameof`、字符串内插 `$""`、表达式体成员 `=>`、`using static`、`out var`、只读自动属性 `{ get; }`。可用 `{ get; private set; }`。
- **不使用 .NET 4.5+ API**：禁 `IReadOnlyList<T>`、`IReadOnlyCollection<T>`、`CultureInfo.DefaultThreadCurrentUICulture`、`Task.Run`。集合暴露用 `ReadOnlyCollection<T>`。
- **编码**：`.cs` / `.csproj` / `.resx` 一律 **UTF-8 with BOM**（`UTF8Encoding($true)`）、CRLF。`.md` 为 UTF-8 无 BOM、LF。
- **代码风格**：Tab 缩进；块式 `namespace`；`using` 写在 `namespace` 内部；公开成员带 XML doc 注释；每文件顶部 `#region copyright` GPLv3 头（照抄任一现有文件，年份行写 `Copyright (C) 2025  The Dual Monitor Tools Chinese translation project`）。
- **只写 HKCU**，绝不写 HKLM。
- **不启动 GUI 做验证**。命令行只能 `DMT.exe -v` / `-?` / `-x`。
- **不改动**：`COPYING.txt`、git 标签 `upstream-2.13`、`DMT\DMT\DMT.csproj.user`、`DmtWallpaper\DmtWallpaper\DmtWallpaper.csproj.user`。
- **验证门槛**：每个任务结束时 `.\build.ps1 -Configuration Release -Test` 必须输出含 `全部构建成功。` 且 NUnit 报 `Failed: 0`。
- **不新增 `NeutralLanguage` 属性**（实测非必需）。
- **不新增任何 `this.Font =` 赋值**（spec 5.1：字体保持系统默认）。

**终止符命名表**（保证跨任务译名一致，实现时严格遵守）：

| 英文 | 中文 | 英文 | 中文 |
|---|---|---|---|
| Monitor | 显示器 | Wallpaper | 壁纸 |
| Screen | 屏幕 | Magic Word | 魔术字 |
| HotKey / Hotkey | 热键 | Snap | 截图 |
| Provider | 提供程序 | Launcher | 启动器 |
| Cursor | 光标 | Options | 选项 |
| General | 常规 | Settings | 设置 |
| Primary | 主 | Working Area | 工作区 |
| Sticky (cursor) | 粘滞 | Dead Space | 死区 |
| System Defined Areas | 系统定义区域 | User Defined Areas | 用户定义区域 |
| Swap Screen | 交换屏幕 | Import / Export | 导入 / 导出 |

## Review Focus

以下五类输入是 spec 隐含但任何单个任务的测试都不会自然覆盖、且最容易让用户踩坑的，其测试分别落在拥有该代码的任务里：

1. **`this.Text = "DMT_GUI_WINDOW"` 被误当作用户文案翻译。** 这个字符串是单实例检测用的窗口标题，`CommandMessaging.FindDmtHWnd()` 靠 `FindWindow(null, "DMT_GUI_WINDOW")` 找它。一旦翻译，第二份 DMT 就能启动，两个托盘实例并存。期望行为：该字面量保持原文不变。→ Task 9 加 `DesignerLiteralGuard_Test`。
2. **含 `{0}` 的格式串译文丢占位符或多占位符。** 17 条既有键含 `{0}`/`{1}`，译文若漏掉会在运行时抛 `FormatException`。期望行为：每条译文的占位符集合与英文完全一致。→ Task 5 的 `PlaceholderParity` 测试覆盖既有键；各 Designer 任务的新键同样纳入该测试（测试是全集扫描，自动覆盖）。
3. **`EditNumericValueForm` 的文案不在 `Designer.cs` 里。** 它是唯一用 `resources.ApplyResources(...)` 的窗体，文案藏在 `EditNumericValueForm.resx` 的 `buttonOK.Text` / `$this.Text` 等条目中。只扫 `.Text = "` 会完全漏掉它，界面会残留英文。期望行为：它的 `zh-Hans` 用**设计器 resx 卫星**（`EditNumericValueForm.zh-Hans.resx`），不是资源类。→ Task 10。
4. **`zh-Hans` resx 漏写或键名拼错导致静默回退英文。** 回退是 CLR 的静默行为，不会报错，很容易误判「已完成」。期望行为：每个中性键都有对应 `zh-Hans` 条目，且无多余键。→ Task 5 的 `CoveragePerResourceClass` 测试（全集扫描）。
5. **`Culture` 未显式设置导致跟随系统区域。** 实测 `Culture=null` 时 `ResourceManager` 退回 `Thread.CurrentUICulture`；本机是 `zh-CN`，则「默认英文」的用户在中文 Windows 上会看到中文。期望行为：`en` 用户在任何区域设置下都拿到英文。→ Task 1 / Task 5 的 `UiCulture_Test`。

---

## 文件结构

**新建（基础设施）**

| 文件 | 职责 |
|---|---|
| `DMT\DMT\Library\Localization\LanguageCatalog.cs` | 语言清单、tag 规范化、`CultureInfo` 解析、读注册表偏好。无 UI 依赖。三工程共享。 |
| `DMT\DMT\Library\Localization\UiCulture.cs` | 把语言应用到当前线程与所有资源类的静态 `Culture`。三工程共享。 |
| `DMT\DMT\Modules\General\LanguageOptionsPanel.cs` | 语言选择面板逻辑。 |
| `DMT\DMT\Modules\General\LanguageOptionsPanel.Designer.cs` | 面板布局（ListBox + 说明文字）。 |
| `DMT\DMT\Modules\General\LanguageOptionsPanel.resx` | 面板设计器资源（仅图标/布局，与项目惯例一致）。 |

**新建（翻译）**

| 文件 | 职责 |
|---|---|
| `DMT\DMT\Resources\{App,Common,Cursor,General,Launcher,Snap,SwapScreen,Wallpaper}Strings.zh-Hans.resx` | 8 个 DMT 资源类的中文卫星。 |
| `DualWallpaper\DualWallpaper\Properties\Resources.zh-Hans.resx` | DualWallpaper 中文卫星。 |
| `DmtWallpaper\DmtWallpaper\Properties\Resources.zh-Hans.resx` | DmtWallpaper 中文卫星。 |
| `DMT\DMT\Modules\General\EditNumericValueForm.zh-Hans.resx` | 该窗体的设计器 resx 中文卫星（见 Review Focus 3）。 |

**新建（测试）**

| 文件 | 职责 |
|---|---|
| `DMT\DMT.UnitTests\Library\Localization\LanguageCatalog_Test.cs` | tag 规范化、查找、默认值。 |
| `DMT\DMT.UnitTests\Library\Localization\UiCulture_Test.cs` | 语言实际生效、`en` 不被系统区域带偏。 |
| `DMT\DMT.UnitTests\Library\Localization\ResourceCoverage_Test.cs` | 覆盖率、占位符一致性、编码、无残留字面量、`DMT_GUI_WINDOW` 守卫。 |

**修改（核心）**

| 文件 | 改动 |
|---|---|
| `DMT\DMT\Library\Environment\DmtRegistry.cs` | `GetLanguage()` / `SetLanguage(string)`。 |
| `DMT\DMT\Program.cs` | `Main` 最早处应用语言。 |
| `DualWallpaper\DualWallpaper\Program.cs` | 同上。 |
| `DmtWallpaper\DmtWallpaper\Program.cs` | 同上。 |
| `DMT\DMT\Modules\General\GeneralModule.cs` | 新增 `Language` 叶节点 + 字段。 |
| `DMT\DMT\DMT.csproj` | 收录 2 个新 .cs、面板三件套、8 个 zh-Hans EmbeddedResource。 |
| `DualWallpaper\DualWallpaper\DualWallpaper.csproj` | 共享 2 个 .cs + 收录 zh-Hans。 |
| `DmtWallpaper\DmtWallpaper\DmtWallpaper.csproj` | 共享 2 个 .cs + `DmtRegistry.cs` + 收录 zh-Hans。 |
| `AGENTS.md` | 第 5.2 节补 `Language` 值；补「不换字体」的实测依据。 |

**修改（Designer 资源化，按模块分任务）**：DMT **57** 个 UI Designer（共 67 个，减去 `Resources\` 下 8 个与 `Properties\` 下 2 个资源类自身）+ 对应中性 resx；DualWallpaper 4 个；DmtWallpaper 5 个。

---

### Task 1: 本地化基础设施

**Files:**
- Create: `DMT\DMT\Library\Localization\LanguageCatalog.cs`
- Create: `DMT\DMT\Library\Localization\UiCulture.cs`
- Modify: `DMT\DMT\Library\Environment\DmtRegistry.cs`
- Create: `DMT\DMT.UnitTests\Library\Localization\LanguageCatalog_Test.cs`
- Modify: `DMT\DMT\DMT.csproj`（收录两个新 .cs）

**Interfaces:**
- Consumes: 无
- Produces（`LanguageDefinition`、`LanguageCatalog`、`UiCulture` 三者均为 **`public`**，因为测试工程在另一个程序集里直接调用）：
  - `DMT.Library.Environment.DmtRegistry.GetLanguage() -> string`（无值/异常时返回 `null`）
  - `DMT.Library.Environment.DmtRegistry.SetLanguage(string languageTag) -> void`
  - `DMT.Library.Localization.LanguageDefinition { string Tag { get; private set; }; string DisplayName { get; private set; }; LanguageDefinition(string tag, string displayName) }`
  - `DMT.Library.Localization.LanguageCatalog.All -> ReadOnlyCollection<LanguageDefinition>`（首项 `en`）
  - `DMT.Library.Localization.LanguageCatalog.Default -> LanguageDefinition`
  - `DMT.Library.Localization.LanguageCatalog.Find(string tag) -> LanguageDefinition`
  - `DMT.Library.Localization.LanguageCatalog.CultureFor(string tag) -> CultureInfo`
  - `DMT.Library.Localization.LanguageCatalog.Normalise(string tagOrNull) -> string`（纯函数，可测）
  - `DMT.Library.Localization.LanguageCatalog.ResolveFromRegistry() -> string`
  - `DMT.Library.Localization.UiCulture.Apply(string languageTag) -> void`

- [ ] **Step 1: 写失败的测试 `LanguageCatalog_Test.cs`**

```csharp
using NUnit.Framework;
using DMT.Library.Localization;

namespace DMT.UnitTests.Library.Localization
{
	[TestFixture]
	public class LanguageCatalog_Test
	{
		[Test]
		public void All_HasEnglishFirstAsDefault()
		{
			Assert.AreEqual("en", LanguageCatalog.All[0].Tag);
			Assert.AreEqual("en", LanguageCatalog.Default.Tag);
			Assert.AreEqual("English", LanguageCatalog.Default.DisplayName);
		}

		[Test]
		public void All_ContainsSimplifiedChinese()
		{
			LanguageDefinition zh = LanguageCatalog.Find("zh-Hans");
			Assert.IsNotNull(zh);
			Assert.AreEqual("简体中文", zh.DisplayName);
		}

		[Test]
		public void Find_UnknownTag_ReturnsDefault()
		{
			Assert.AreEqual("en", LanguageCatalog.Find("kl-GL").Tag);
			Assert.AreEqual("en", LanguageCatalog.Find(null).Tag);
			Assert.AreEqual("en", LanguageCatalog.Find(string.Empty).Tag);
		}

		[Test]
		public void Find_IsCaseInsensitive()
		{
			Assert.AreEqual("zh-Hans", LanguageCatalog.Find("ZH-HANS").Tag);
		}

		// Review Focus 5: a null/invalid/absent preference must land on "en",
		// never on the machine's regional culture.
		[TestCase(null, "en")]
		[TestCase("", "en")]
		[TestCase("   ", "en")]
		[TestCase("en", "en")]
		[TestCase("zh-Hans", "zh-Hans")]
		[TestCase("zh-hans", "zh-Hans")]
		[TestCase("zh-CN", "zh-Hans")]
		[TestCase("zh-TW", "en")]
		[TestCase("kl-GL", "en")]
		public void Normalise_MapsPreferenceToSupportedTag(string input, string expected)
		{
			Assert.AreEqual(expected, LanguageCatalog.Normalise(input));
		}

		[Test]
		public void CultureFor_ReturnsRequestedCulture()
		{
			Assert.AreEqual("zh-Hans", LanguageCatalog.CultureFor("zh-Hans").Name);
			Assert.AreEqual("en", LanguageCatalog.CultureFor("en").Name);
			Assert.AreEqual("en", LanguageCatalog.CultureFor("kl-GL").Name);
		}
	}
}
```

`Normalise` 的映射规则（测试即规格）：去空白；忽略大小写匹配 `All` 里的 tag；`zh-*` 且非 `zh-TW`/`zh-HK`/`zh-MO` 时归一到 `zh-Hans`；其余一律 `en`。

- [ ] **Step 2: 运行测试确认失败**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: 编译失败，`CS0246: 未能找到类型或命名空间名"LanguageCatalog"`。

- [ ] **Step 3: 实现 `LanguageCatalog.cs`**

`LanguageCatalog` 的 `All` 用静态构造填充 `List<LanguageDefinition>` 再包 `ReadOnlyCollection<LanguageDefinition>`；`Normalise` 按 Step 1 的规则实现；`ResolveFromRegistry()` 返回 `Normalise(DmtRegistry.GetLanguage())`。

- [ ] **Step 4: 在 `DmtRegistry.cs` 加语言读写**

仿现有 `SetDmtWallpaperFilename` 的写法：新增 `const string LanguageValueName = "Language";`、`GetLanguage()`（`OpenSubKey` + `GetValue`，键或值缺失、任何异常都返回 `null`）、`SetLanguage(string)`（`CreateSubKey` + `SetValue` + `Close`）。

- [ ] **Step 5: 实现 `UiCulture.cs`**

`Apply` 做三件事：`new CultureInfo(LanguageCatalog.Normalise(tag))` → 设 `Thread.CurrentThread.CurrentUICulture` → 用 `typeof(UiCulture).Assembly.GetTypes()` 遍历、`GetProperty("Culture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)`、类型为 `CultureInfo` 且 `CanWrite` 的逐个 `SetValue(null, culture, null)`，全部包在 try/catch 内静默失败。

关键点：`UiCulture.cs` 会被 `Compile Include` 进三个程序集，`typeof(UiCulture).Assembly` 因此在每个程序里都指向**它自己所在的那个程序集**，天然正确，不需要参数传入。

- [ ] **Step 6: 收录进 `DMT.csproj`**

在 `<ItemGroup>` 的 Compile 区加两行 `<Compile Include="Library\Localization\LanguageCatalog.cs" />` 与 `Library\Localization\UiCulture.cs`。用 `UTF8Encoding($true)` 写回，确认 BOM 仍在。

- [ ] **Step 7: 运行测试确认通过**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `全部构建成功。`，`Failed: 0`，`LanguageCatalog_Test` 全绿。

- [ ] **Step 8: 提交**

```bash
git add DMT/DMT/Library/Localization DMT/DMT/Library/Environment/DmtRegistry.cs DMT/DMT/DMT.csproj DMT/DMT.UnitTests/Library/Localization
git commit -m "feat(i18n): add language catalog, culture applier and registry preference"
```

---

### Task 2: 翻译既有键 — AppStrings / CommonStrings / GeneralStrings

**Files:**
- Create: `DMT\DMT\Resources\AppStrings.zh-Hans.resx`（2 键）
- Create: `DMT\DMT\Resources\CommonStrings.zh-Hans.resx`（24 键）
- Create: `DMT\DMT\Resources\GeneralStrings.zh-Hans.resx`（12 键）
- Modify: `DMT\DMT\DMT.csproj`（3 条 `EmbeddedResource`）

**Interfaces:**
- Consumes: Task 1 的 `LanguageCatalog`（运行时由 `Culture` 选择 resx，本任务不需要代码引用）
- Produces: 3 个 `zh-Hans` 卫星 resx，键集与对应中性 resx 完全一致

- [ ] **Step 1: 建三个 zh-Hans resx**

对每个中性 resx：完整复制文件 → 删除全部 `<data>` 条目 → 按中性 resx 的键顺序重新加入同名条目，`<value>` 写中文译文。保留所有 `<resheader>` 与文件头注释块原样。

- [ ] **Step 2: 在 `DMT.csproj` 收录**

在每个中性 resx 的 `<EmbeddedResource>` 之后追加：

```xml
<EmbeddedResource Include="Resources\AppStrings.zh-Hans.resx">
  <DependentUpon>AppStrings.resx</DependentUpon>
</EmbeddedResource>
```

三个文件各一段。用 `UTF8Encoding($true)` 写回。

- [ ] **Step 3: 构建并验证卫星程序集生成**

Run: `.\build.ps1 -Configuration Release`
Expected: `全部构建成功。`；`DMT\DMT\bin\Release\zh-Hans\DMT.resources.dll` 存在。

- [ ] **Step 4: 逐键核对占位符**

Run:
```powershell
cd D:\workapsce\common\DMT
foreach ($n in 'AppStrings','CommonStrings','GeneralStrings') {
  [xml]$en = Get-Content "DMT\DMT\Resources\$n.resx" -Raw -Encoding UTF8
  [xml]$zh = Get-Content "DMT\DMT\Resources\$n.zh-Hans.resx" -Raw -Encoding UTF8
  foreach ($d in @($en.root.data)) {
    $z = @($zh.root.data) | Where-Object { $_.name -eq $d.name }
    if (-not $z) { "MISSING $n.$($d.name)"; continue }
    $pe = ([regex]::Matches($d.value,'\{\d+\}') | ForEach-Object { $_.Value } | Sort-Object) -join ','
    $pz = ([regex]::Matches($z.value,'\{\d+\}') | ForEach-Object { $_.Value } | Sort-Object) -join ','
    if ($pe -ne $pz) { "PLACEHOLDER MISMATCH $n.$($d.name): en=[$pe] zh=[$pz]" }
  }
  "checked $n"
}
```
Expected: 每个文件只打印 `checked <name>`，无 `MISSING` / `PLACEHOLDER MISMATCH`。

- [ ] **Step 5: 提交**

```bash
git add DMT/DMT/Resources/*.zh-Hans.resx DMT/DMT/DMT.csproj
git commit -m "i18n: add zh-Hans satellites for AppStrings, CommonStrings, GeneralStrings"
```

---

### Task 3: 翻译既有键 — CursorStrings / LauncherStrings / SnapStrings

**Files:**
- Create: `DMT\DMT\Resources\CursorStrings.zh-Hans.resx`（14 键）
- Create: `DMT\DMT\Resources\LauncherStrings.zh-Hans.resx`（18 键）
- Create: `DMT\DMT\Resources\SnapStrings.zh-Hans.resx`（9 键）
- Modify: `DMT\DMT\DMT.csproj`（3 条 `EmbeddedResource`）

**Interfaces:**
- Consumes: Task 2 建立的 zh-Hans resx 格式与 csproj 收录写法
- Produces: 3 个 `zh-Hans` 卫星 resx

步骤与 Task 2 完全同构，只是文件与键集不同。执行 Task 2 的 Step 1–5，把文件名替换为这三个，Step 4 的 `$n` 数组改为 `'CursorStrings','LauncherStrings','SnapStrings'`。

提交信息：`i18n: add zh-Hans satellites for CursorStrings, LauncherStrings, SnapStrings`

---

### Task 4: 翻译既有键 — SwapScreenStrings / WallpaperStrings

**Files:**
- Create: `DMT\DMT\Resources\SwapScreenStrings.zh-Hans.resx`（39 键）
- Create: `DMT\DMT\Resources\WallpaperStrings.zh-Hans.resx`（24 键）
- Modify: `DMT\DMT\DMT.csproj`（2 条 `EmbeddedResource`）

**Interfaces:**
- Consumes: 同 Task 3
- Produces: 2 个 `zh-Hans` 卫星 resx

步骤同构 Task 2，文件为这两个，Step 4 的 `$n` 数组改为 `'SwapScreenStrings','WallpaperStrings'`。

提交信息：`i18n: add zh-Hans satellites for SwapScreenStrings, WallpaperStrings`

---

### Task 5: 覆盖率与质量测试

**Files:**
- Create: `DMT\DMT.UnitTests\Library\Localization\ResourceCoverage_Test.cs`
- Create: `DMT\DMT.UnitTests\Library\Localization\UiCulture_Test.cs`

**Interfaces:**
- Consumes: Task 1 的 `LanguageCatalog` / `UiCulture`；Task 2–4 的 8 个 zh-Hans resx
- Produces: 后续所有 Designer 任务靠这两个测试的**全集扫描**自动获得覆盖保护，无需各自新增

- [ ] **Step 1: 写 `ResourceCoverage_Test.cs`**

测试通过 `AppDomain.CurrentDomain.BaseDirectory` 定位仓库。从 `DMT\DMT.UnitTests\bin\Release\` 向上 4 级即仓库根（`Release→bin→DMT.UnitTests→DMT→DMT`，已实测）。用 `XmlDocument` 解析 resx（**不用文本正则**：resx 头部注释块里示范了 `Name1`/`Color1`/`Bitmap1`/`Icon1` 四个假 `<data>`，正则会误算，XML 解析不会）。

在测试类里放这个共享辅助（Task 9 与 Task 18 也复用它）：

```csharp
// Repo root = the folder that contains DMT\, DualWallpaper\, DmtWallpaper\.
// From bin\Release\ we go up three levels to reach DMT\ (the project root's child),
// so four levels reach the repository root.
static readonly string RepoRoot =
    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));

static string ResourcesDir
{
    get { return Path.Combine(RepoRoot, @"DMT\DMT\Resources"); }
}
```

四组断言：

```csharp
static readonly string[] DmtResourceClasses = {
    "AppStrings", "CommonStrings", "CursorStrings", "GeneralStrings",
    "LauncherStrings", "SnapStrings", "SwapScreenStrings", "WallpaperStrings"
};

// Review Focus 4: every neutral key must have a zh-Hans entry, and no extras.
[Test]
public void CoveragePerResourceClass(
    [ValueSource("DmtResourceClasses")] string cls)
{
    // 断言 zh-Hans 键集 == 中性键集（对称差为空）
}

// Review Focus 2: placeholder sets must match exactly.
[Test]
public void PlaceholderParity(
    [ValueSource("DmtResourceClasses")] string cls)
{
    // 对每个共同键，正则 \{\d+\} 的排序后集合必须相等
}

// 译文不得为空，也不得与英文完全相同（少数专有名词除外，见白名单）
[Test]
public void TranslationsAreTranslated(
    [ValueSource("DmtResourceClasses")] string cls)
{
    // 白名单：Dual Monitor Tools（产品名）、COPYING.txt
}

// 编码：zh-Hans resx 必须是 UTF-8 with BOM
[Test]
public void ZhHansResxIsUtf8WithBom(
    [ValueSource("DmtResourceClasses")] string cls)
{
    // 首三字节 EF BB BF
}
```

`ValueSource` 需要 `static` 只读字段，注意 NUnit 3.7.1 要求字段而非属性。

- [ ] **Step 2: 写 `UiCulture_Test.cs`**

```csharp
using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using DMT.Library.Localization;

namespace DMT.UnitTests.Library.Localization
{
	[TestFixture]
	public class UiCulture_Test
	{
		static PropertyInfo CultureOf(string typeFullName)
		{
			Type t = typeof(UiCulture).Assembly.GetType(typeFullName, false);
			Assert.IsNotNull(t, typeFullName + " not found");
			PropertyInfo p = t.GetProperty("Culture",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(p, typeFullName + ".Culture not found");
			return p;
		}

		static readonly string[] ResourceClassNames = {
			"DMT.Properties.Resources",
			"DMT.Resources.AppStrings",
			"DMT.Resources.CommonStrings",
			"DMT.Resources.CursorStrings",
			"DMT.Resources.GeneralStrings",
			"DMT.Resources.LauncherStrings",
			"DMT.Resources.SnapStrings",
			"DMT.Resources.SwapScreenStrings",
			"DMT.Resources.WallpaperStrings"
		};

		// Review Focus 5: "en" must win even when the OS culture is Chinese.
		[Test]
		public void Apply_EnglishWinsOverSystemChineseCulture()
		{
			CultureInfo saved = Thread.CurrentThread.CurrentUICulture;
			try
			{
				Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
				UiCulture.Apply("en");

				Assert.AreEqual("en",
					((CultureInfo)CultureOf("DMT.Resources.AppStrings").GetValue(null, null)).Name);
				Assert.AreEqual(new CultureInfo("en"),
					Thread.CurrentThread.CurrentUICulture);
			}
			finally
			{
				Thread.CurrentThread.CurrentUICulture = saved;
				UiCulture.Apply("en");   // restore resource-class state for other fixtures
			}
		}

		[Test]
		public void Apply_SetsCultureOnEveryResourceClass()
		{
			try
			{
				UiCulture.Apply("zh-Hans");
				foreach (string name in ResourceClassNames)
				{
					CultureInfo c = (CultureInfo)CultureOf(name).GetValue(null, null);
					Assert.AreEqual("zh-Hans", c.Name, name + " culture not applied");
				}
			}
			finally
			{
				UiCulture.Apply("en");
			}
		}

		// An unsupported tag must degrade to English, not to the OS culture.
		[Test]
		public void Apply_UnknownTagFallsBackToEnglish()
		{
			try
			{
				UiCulture.Apply("kl-GL");
				Assert.AreEqual("en",
					((CultureInfo)CultureOf("DMT.Resources.AppStrings").GetValue(null, null)).Name);
			}
			finally
			{
				UiCulture.Apply("en");
			}
		}
	}
}
```

`UiCulture` 与 `LanguageCatalog` 因此必须是 `public`（测试工程在另一个程序集里调用）。资源类本身是 `internal`，用反射按类型全名取值绕过，不需要 `InternalsVisibleTo`。

- [ ] **Step 3: 运行测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `Failed: 0`。若 `TranslationsAreTranslated` 失败，说明该键还在用英文原文，回 Task 2–4 补译。

- [ ] **Step 4: 提交**

```bash
git add DMT/DMT.UnitTests/Library/Localization
git commit -m "test(i18n): assert zh-Hans coverage, placeholder parity and culture application"
```

---

### Task 6: DMT 启动接线 + Language 选项面板

**Files:**
- Modify: `DMT\DMT\Program.cs`（`Main` 开头）
- Create: `DMT\DMT\Modules\General\LanguageOptionsPanel.cs`
- Create: `DMT\DMT\Modules\General\LanguageOptionsPanel.Designer.cs`
- Create: `DMT\DMT\Modules\General\LanguageOptionsPanel.resx`
- Modify: `DMT\DMT\Modules\General\GeneralModule.cs`
- Modify: `DMT\DMT\Resources\GeneralStrings.resx` + `GeneralStrings.Designer.cs` + `GeneralStrings.zh-Hans.resx`
- Modify: `DMT\DMT\DMT.csproj`

**Interfaces:**
- Consumes: `LanguageCatalog.All` / `.Default` / `.ResolveFromRegistry()`（Task 1）、`UiCulture.Apply`（Task 1）、`DmtRegistry.GetLanguage` / `SetLanguage`（Task 1）
- Produces: `DMT.Modules.General.LanguageOptionsPanel : UserControl`，无参构造；`GeneralStrings.Node_Language`、`GeneralStrings.LanguageOptionsPanel_labelHeading_Text`、`GeneralStrings.LanguageOptionsPanel_labelHint_Text`、`GeneralStrings.LanguageOptionsPanel_labelRestartNeeded_Text` 四个新键

- [ ] **Step 1: 在 `Program.cs` 的 `Main` 最前接线**

`Main` 内第一行（`ProgramOptions` 之前）加：

```csharp
UiCulture.Apply(LanguageCatalog.ResolveFromRegistry());
```

位置必须在**任何窗体创建之前**。`ConsoleMain` 路径也经过这里，不影响 `-v` / `-?` / `-x` 的输出。

- [ ] **Step 2: 建 `LanguageOptionsPanel.Designer.cs`**

仿 `GeneralRootOptionsPanel.Designer.cs` 的结构（`AutoScaleDimensions = new SizeF(6F, 13F)`、`AutoScaleMode = AutoScaleMode.Font`、`Size = new Size(500, 432)`）。控件：

- `labelHeading`：`AutoSize = true`，`Font` 为 12pt（与 `GeneralRootOptionsPanel.label1` 同款），`Location = (25, 25)`，`Text` 引用 `GeneralStrings.LanguageOptionsPanel_labelHeading_Text`
- `listBoxLanguages`：`Location = (25, 60)`，`Size = (300, 120)`，`IntegralHeight = false`
- `labelHint`：`AutoSize = true`，`Location = (25, 195)`，`Text` 引用 `..._labelHint_Text`
- `labelRestartNeeded`：`AutoSize = true`，`Location = (25, 220)`，`Text` 引用 `..._labelRestartNeeded_Text`，初始 `Visible = false`

父容器 `AutoSize` 一律不在 Designer 里新增（Global Constraints 禁止布局改动），只设各控件的 `AutoSize` 属性，与现有面板一致。

- [ ] **Step 3: 建 `LanguageOptionsPanel.cs`**

构造函数：`InitializeComponent()` → 填充 `listBoxLanguages`（遍历 `LanguageCatalog.All`，`Items.Add(lang.DisplayName)`，`Tag` 存 `LanguageDefinition`）→ 选中当前语言（`LanguageCatalog.Default` 与 `Find(LanguageCatalog.ResolveFromRegistry())`）→ 记录启动语言到 `_startupLanguageTag`。

`listBoxLanguages.SelectedIndexChanged`：`_initialising` 为真时直接返回；否则取选中项的 `Tag as LanguageDefinition`，`DmtRegistry.SetLanguage(def.Tag)`，然后 `labelRestartNeeded.Visible = (def.Tag != _startupLanguageTag)`。

用 `_initialising` 布尔量防初始化期误触发，与 `GeneralOptionsPanel` 同款。

- [ ] **Step 4: 在 `GeneralStrings.resx` 加 4 个键并同步 Designer 属性**

往 `GeneralStrings.resx` 加：

```xml
<data name="Node_Language" xml:space="preserve"><value>Language</value></data>
<data name="LanguageOptionsPanel_labelHeading_Text" xml:space="preserve"><value>Language</value></data>
<data name="LanguageOptionsPanel_labelHint_Text" xml:space="preserve"><value>Select the language used for the user interface.</value></data>
<data name="LanguageOptionsPanel_labelRestartNeeded_Text" xml:space="preserve"><value>The language has been saved. Please restart Dual Monitor Tools to apply the change.</value></data>
```

同时在 `GeneralStrings.Designer.cs` 手写 4 个属性（`ResXFileCodeGenerator` 只在 VS 里跑，命令行构建不会重新生成），格式照抄同文件既有属性：

```csharp
internal static string Node_Language {
    get {
        return ResourceManager.GetString("Node_Language", resourceCulture);
    }
}
```

并往 `GeneralStrings.zh-Hans.resx` 加这 4 个键的中文（`语言` / `语言` / `选择用户界面使用的语言。` / `语言已保存。请重启 Dual Monitor Tools 以应用更改。`）。

- [ ] **Step 5: 在 `GeneralModule.cs` 挂节点**

加字段 `LanguageOptionsPanel _languageOptionsPanel;`（紧随 L62 的 `_generalMonitorsOptionsPanel`）。在 `GetOptionNodes()` 的 `if (_localEnvironment.IsWin7OrLater())` 块**之后**、`return options;` 之前加：

```csharp
_languageOptionsPanel = new LanguageOptionsPanel();
options.Nodes.Add(new ModuleOptionNodeLeaf(GeneralStrings.Node_Language, image, _languageOptionsPanel));
```

放在 if 块外，保证非 Win7+ 时该节点依然存在。

- [ ] **Step 6: `DMT.csproj` 收录**

加 3 条 Compile（`LanguageOptionsPanel.cs`、`.Designer.cs`，后者带 `<AutoGen>True</AutoGen><DesignTime>True</DesignTime><DependentUpon>LanguageOptionsPanel.resx</DependentUpon>`）与 1 条 `<EmbeddedResource Include="Modules\General\LanguageOptionsPanel.resx"><DependentUpon>LanguageOptionsPanel.cs</DependentUpon></EmbeddedResource>`。BOM 保持。

- [ ] **Step 7: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `全部构建成功。`，`Failed: 0`。

再验证命令行路径未被破坏（不加参数就跑 GUI，会产生持久副作用，禁止）：

Run: `.\DMT\DMT\bin\Release\DMT.exe -v`
Expected: 打印 `DMT 2.13.0.0` 后退出。

- [ ] **Step 8: 提交**

```bash
git add DMT/DMT/Program.cs DMT/DMT/Modules/General DMT/DMT/Resources DMT/DMT/DMT.csproj
git commit -m "feat(i18n): wire language selection into DMT startup and options tree"
```

---

### Task 7: DualWallpaper 接线 + 中文卫星

**Files:**
- Modify: `DualWallpaper\DualWallpaper\Program.cs`
- Create: `DualWallpaper\DualWallpaper\Properties\Resources.zh-Hans.resx`（10 键）
- Modify: `DualWallpaper\DualWallpaper\DualWallpaper.csproj`

**Interfaces:**
- Consumes: Task 1 的 `LanguageCatalog` / `UiCulture` / `DmtRegistry`
- Produces: 无新 API

- [ ] **Step 1: `Program.cs` 接线**

`Main` 第一行加 `UiCulture.Apply(LanguageCatalog.ResolveFromRegistry());`。该文件当前 `using` 在 namespace 外（`using DualWallpaper.Properties;` 在顶部），按现有风格追加 `using DMT.Library.Localization;` 到顶部 using 区。

- [ ] **Step 2: `csproj` 共享两个源文件**

在 Compile 区加：

```xml
<Compile Include="..\..\DMT\DMT\Library\Localization\LanguageCatalog.cs">
  <Link>Library\Localization\LanguageCatalog.cs</Link>
</Compile>
<Compile Include="..\..\DMT\DMT\Library\Localization\UiCulture.cs">
  <Link>Library\Localization\UiCulture.cs</Link>
</Compile>
```

`<Link>` 让它们在 IDE 里归到 `Library\Localization` 而非仓库根。`DmtRegistry.cs` 已在本工程共享，无需再加。

- [ ] **Step 3: 建 `Properties\Resources.zh-Hans.resx`**

10 条字符串键全部翻译（`AboutMenuItem` / `Center` / `License` / `MyTitle` / `OpenImageFilter` / `OverStretch` / `SaveImageFilter` / `Stretch` / `UnderStretch` / `WebsiteMenuItem`）。

- `License` 保留法务含义：`本程序不提供任何担保。这是自由软件，欢迎在特定条件下再分发；详情请见 COPYING.txt 文件。`
- `OpenImageFilter` / `SaveImageFilter` 是文件对话框过滤器，格式 `描述|*.ext` 的**结构不能变**，只译描述部分，`|` 与扩展名列表原样保留。
- `MyTitle` 保持 `Dual Wallpaper`（产品名不译）。

- [ ] **Step 4: `csproj` 收录 zh-Hans**

```xml
<EmbeddedResource Include="Properties\Resources.zh-Hans.resx">
  <DependentUpon>Resources.resx</DependentUpon>
</EmbeddedResource>
```

- [ ] **Step 5: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `全部构建成功。`，`Failed: 0`；`DualWallpaper\DualWallpaper\bin\Release\zh-Hans\DualWallpaper.resources.dll` 存在。

- [ ] **Step 6: 提交**

```bash
git add DualWallpaper/DualWallpaper
git commit -m "feat(i18n): wire language selection into DualWallpaper"
```

---

### Task 8: DmtWallpaper 接线 + 中文卫星

**Files:**
- Modify: `DmtWallpaper\DmtWallpaper\Program.cs`
- Create: `DmtWallpaper\DmtWallpaper\Properties\Resources.zh-Hans.resx`（2 键：`MyTitle`、`License`）
- Modify: `DmtWallpaper\DmtWallpaper\DmtWallpaper.csproj`

**Interfaces:**
- Consumes: Task 1 的三件基础设施
- Produces: 无新 API

- [ ] **Step 1: `Program.cs` 接线**

`Main` 第一行（`Application.EnableVisualStyles()` 之前）加 `UiCulture.Apply(LanguageCatalog.ResolveFromRegistry());`，并加 `using DMT.Library.Localization;`。

- [ ] **Step 2: `csproj` 共享三个源文件**

加 `LanguageCatalog.cs`、`UiCulture.cs`（同 Task 7 的 `<Link>` 写法）**以及** `DmtRegistry.cs`：

```xml
<Compile Include="..\..\DMT\DMT\Library\Environment\DmtRegistry.cs">
  <Link>Library\Environment\DmtRegistry.cs</Link>
</Compile>
```

`DmtRegistry.cs` 只 `using Microsoft.Win32` + `System*`，已实测在 v4.0 Client Profile 下无需额外引用即可编译。它与本工程既有的 `WallpaperFilenameHelper.cs` 读同一个注册表键，不冲突。

- [ ] **Step 3: 建 zh-Hans resx 并收录**

两条字符串键：`MyTitle` 保持 `DmtWallpaper`（产品名），`License` 译文同 Task 7。不含那 2 条 `ResXFileRef` PNG 条目——缺图时 CLR 自动回退中性 resx。

csproj 加 `EmbeddedResource`（同 Task 7 Step 4）。

- [ ] **Step 4: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `全部构建成功。`，`Failed: 0`；`DmtWallpaper\DmtWallpaper\bin\Release\zh-Hans\DmtWallpaper.resources.dll` 存在；`DmtWallpaper.scr` 仍由 PostBuildEvent 生成。

- [ ] **Step 5: 提交**

```bash
git add DmtWallpaper/DmtWallpaper
git commit -m "feat(i18n): wire language selection into DmtWallpaper"
```

---

### Task 9: 资源化 DMT 主窗体（AppForm / OptionsForm / AboutForm）

**Files:**
- Modify: `DMT\DMT\AppForm.cs:67-70`（4 处 `AddMenuItem`）
- Modify: `DMT\DMT\AppForm.Designer.cs`（3 处 `.Text`）
- Modify: `DMT\DMT\OptionsForm.Designer.cs`（2 处 `.Text`）
- Modify: `DMT\DMT\AboutForm.Designer.cs` + `AboutForm.cs`（5 处 `.Text`）
- Modify: `DMT\DMT\Resources\AppStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`
- Modify: `DMT\DMT.UnitTests\Library\Localization\ResourceCoverage_Test.cs`（加守卫测试）

**Interfaces:**
- Consumes: `AppStrings` 资源类（现有 2 键 + 本任务新增）
- Produces: 新增 `AppStrings` 键，命名 `AppForm_optionsToolStripMenuItem_Text` 等；`AppForm` 的 `DMT_GUI_WINDOW` 保持不变

- [ ] **Step 1: 先写守卫测试（Review Focus 1）**

在 `ResourceCoverage_Test.cs` 加：

```csharp
// Review Focus 1: the single-instance window title must never be translated.
// CommandMessaging.FindDmtHWnd() looks it up with FindWindow(null, "DMT_GUI_WINDOW").
[Test]
public void AppFormWindowTitleStaysUntranslated()
{
    string designer = Path.Combine(RepoRoot, @"DMT\DMT\AppForm.Designer.cs");
    string text = File.ReadAllText(designer);
    StringAssert.Contains(@"this.Text = ""DMT_GUI_WINDOW"";", text);
}

// Review Focus 4 (broader): no user-visible literal may remain in DMT UI designers.
// Runs over every *.Designer.cs under DMT\DMT except Resources / Properties.
[Test]
public void NoLiteralTextRemainsInDmtDesigners()
{
    // 扫地：\bthis\.\w+(\.\w+)*\.Text = "  ... ;
    // 白名单：DMT_GUI_WINDOW（窗口标题）、label7 / label9 / labelDescription /
    //         labelLicence / labelCopyright（设计期占位符，运行时被代码覆盖）
}
```

后一条在整个 DMT 资源化完成前会失败。**本任务先只加 `AppFormWindowTitleStaysUntranslated`**（它此刻就应该通过），`NoLiteralTextRemainsInDmtDesigners` 留到 Task 18 再加。

- [ ] **Step 2: 资源化 4 处菜单项**

`AppForm.cs:67-70` 把 `"About"` / `"Visit Website"` / `"-"` / `"Exit"` 改为 `AppStrings.AppForm_aboutToolStripMenuItem_Text` / `..._visitWebSiteToolStripMenuItem_Text` / `"-"`（分隔符不译，保持字面量）/ `..._exitToolStripMenuItem_Text`。

- [ ] **Step 3: 资源化 3 个窗体的 `.Text`**

`AppForm.Designer.cs`：`notifyIcon.Text = "Dual Monitor Tools"` → `AppStrings.AppForm_notifyIcon_Text`（产品名，中文值同为 `Dual Monitor Tools`）；`optionsToolStripMenuItem.Text = "Options"` → `AppStrings.AppForm_optionsToolStripMenuItem_Text`（中文 `选项`）；`this.Text = "DMT_GUI_WINDOW"` **不动**。

`OptionsForm.Designer.cs`：`buttonClose.Text = "Close"` → `AppStrings.OptionsForm_buttonClose_Text`；`this.Text = "Options for Dual Monitor Tools"` → `AppStrings.OptionsForm_Text`（中文 `Dual Monitor Tools 选项`）。

`AboutForm.Designer.cs` + `AboutForm.cs`：`.Text` 字面量 5 处按同规则处理，其中 `AboutForm.cs` 里 `string.Format("About {0}", ...)` 与 `"{0}  -  Version {1}"` 两条改为资源串，**保留 `{0}` / `{1}` 占位符原样**（中文可用 `关于 {0}`、`{0}  -  版本 {1}`）。

- [ ] **Step 4: 补 resx（中性英文 + Designer 属性 + 中文）**

三条同步：`AppStrings.resx` 加 `<data>`；`AppStrings.Designer.cs` 手写属性；`AppStrings.zh-Hans.resx` 加译文。键名严格用 Step 2–3 里写的形式。

- [ ] **Step 5: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `Failed: 0`，`AppFormWindowTitleStaysUntranslated` 绿。

- [ ] **Step 6: 提交**

```bash
git add DMT/DMT/AppForm.cs DMT/DMT/AppForm.Designer.cs DMT/DMT/OptionsForm.Designer.cs DMT/DMT/AboutForm.cs DMT/DMT/AboutForm.Designer.cs DMT/DMT/Resources DMT/DMT.UnitTests/Library/Localization
git commit -m "i18n: resource-ize DMT main window, options dialog and about box"
```

---

### Task 10: 资源化 General 模块 + EditNumericValueForm 的 ApplyResources 处理

**Files:**
- Modify: `DMT\DMT\Modules\General\GeneralModule.cs`（5 处节点标签）
- Modify: `DMT\DMT\Modules\General\{GeneralOptionsPanel,GeneralRootOptionsPanel,GeneralMonitorNamesOptionsPanel,GeneralMonitorOrderingOptionsPanel,GeneralMonitorsOptionsPanel,CheckUpdatesForm,EditMonitorNameForm,ChangeOrientationForm}.Designer.cs`（共 ~60 处 `.Text`；该目录 9 个 Designer 中的 8 个，第 9 个 `EditNumericValueForm` 单独处理）
- Create: `DMT\DMT\Modules\General\EditNumericValueForm.zh-Hans.resx`
- Modify: `DMT\DMT\Modules\General\EditNumericValueForm.resx`（不动内容，仅确认键名）
- Modify: `DMT\DMT\Resources\GeneralStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`
- Modify: `DMT\DMT\DMT.csproj`（收录 `EditNumericValueForm.zh-Hans.resx`）

**Interfaces:**
- Consumes: `GeneralStrings`（Task 6 已加 4 键）
- Produces: 新增 `GeneralStrings` 键，节点用 `Node_*`，控件用 `GeneralXxxPanel_<控件名>_Text`

- [ ] **Step 1: 5 处选项树节点标签**

`GeneralModule.cs` 的 `"Dual Monitor Tools"`（分支根，产品名，中文保持原文）、`"General"`（`常态`→`常规`）、`"Monitor Names"`（`显示器名称`）、`"Monitor Order"`（`显示器顺序`）、`"Monitors"`（`显示器`）改为 `GeneralStrings.Node_*`。

- [ ] **Step 2: 资源化 8 个 Designer 的 `.Text`**

逐个把 `this.<控件>.Text = "..."` 换成 `GeneralStrings.<面板名>_<控件名>_Text`。注意：

- `GeneralRootOptionsPanel.label1.Text = "Dual Monitor Tools"` 是产品名，资源化但中文值保持 `Dual Monitor Tools`。
- 6 处设计期占位符**保持原样不改**（运行时被构造函数覆盖）：`GeneralMonitorNamesOptionsPanel.labelBrand.Text = "label3"`、`labelGpu.Text = "label5"`、`labelSize.Text = "label7"`、`GeneralOptionsPanel.labelInstallType.Text = "label9"`、`labelVersion.Text = "label7"`、`ParameterInputForm.labelPrompt.Text = "label1"`（后者属 Task 12）。此外 `GeneralOptionsPanel.labelDescription` / `labelLicence` / `labelCopyright` 在 `.cs` 里被赋值，也保持原样。

- [ ] **Step 3: 处理 `EditNumericValueForm` 的设计器 resx（Review Focus 3）**

该窗体用 `resources.ApplyResources(ctrl, "name")`，文案在 `EditNumericValueForm.resx`。**不要**改 `.Designer.cs`。新建 `EditNumericValueForm.zh-Hans.resx`，只放需要翻译的 `.Text` 条目：

```xml
<data name="buttonOK.Text" xml:space="preserve"><value>确定</value></data>
<data name="buttonCancel.Text" xml:space="preserve"><value>取消</value></data>
<data name="labelCurBrightness.Text" xml:space="preserve"><value>当前</value></data>
<data name="labelMinBrightness.Text" xml:space="preserve"><value>最小</value></data>
<data name="labelMaxBrightness.Text" xml:space="preserve"><value>最大</value></data>
<data name="$this.Text" xml:space="preserve"><value>调整显示器亮度</value></data>
```

**绝对不要**复制 `Location` / `Size` / `AutoScaleDimensions` / `ClientSize` / `TabIndex` 这类布局条目——一旦复制，中文 resx 就成了布局的第二来源，与「不改布局」冲突。缺布局条目时 `ApplyResources` 自动回退中性 resx。

已实测：这种部分条目做法能让 `zh-Hans` 拿到中文文字、同时布局完全继承中性 resx。

- [ ] **Step 4: csproj 收录 `EditNumericValueForm.zh-Hans.resx`**

```xml
<EmbeddedResource Include="Modules\General\EditNumericValueForm.zh-Hans.resx">
  <DependentUpon>EditNumericValueForm.resx</DependentUpon>
</EmbeddedResource>
```

- [ ] **Step 5: 补 `GeneralStrings` 三条同步**

中性 resx 加英文 `<data>`；`GeneralStrings.Designer.cs` 手写属性；`zh-Hans.resx` 加中文。

- [ ] **Step 6: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `Failed: 0`（`CoveragePerResourceClass` / `PlaceholderParity` 会发现任何遗漏或占位符不一致）。

- [ ] **Step 7: 提交**

```bash
git add DMT/DMT/Modules/General DMT/DMT/Resources DMT/DMT/DMT.csproj
git commit -m "i18n: resource-ize General module panels and EditNumericValueForm satellite"
```

---

### Task 11: 资源化 Cursor 模块

**Files:**
- Modify: `DMT\DMT\Modules\Cursor\CursorModule.cs`（5 处节点标签）
- Modify: `DMT\DMT\Modules\Cursor\{CursorRootOptionsPanel,CursorGeneralOptionsPanel,CursorStickyOptionsPanel,CursorPrimaryOptionsPanel,CursorDeadSpaceOptionsPanel}.Designer.cs`（约 31 处 `.Text`，含 5 处多行拼接）
- Modify: `DMT\DMT\Resources\CursorStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`

**Interfaces:**
- Consumes: `CursorStrings`
- Produces: 新增 `CursorStrings` 键

- [ ] **Step 1: 5 处节点标签**

`"Cursor"`（`光标`）、`"General"`（`常规`）、`"Sticky"`（`粘滞`）、`"Primary Working Area"`（`主工作区`）、`"Dead Space"`（`死区`）。

- [ ] **Step 2: 资源化 `.Text`，正确处理多行拼接**

有 5 处形如：

```csharp
this.label3.Text = "These options allow you to move the cursor through such borders and the cursor wi" +
    "ll jump to the nearest valid position on the adjoining mointor in the direction " +
    "that the cursor is being moved.";
```

这类**先把英文拼成一条完整句子写进 resx，再把整段替换成单个资源引用**。不要把中译文按原来 80 列的位置切断。

- [ ] **Step 3: 补 `CursorStrings` 三条同步**

- [ ] **Step 4: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `Failed: 0`。

- [ ] **Step 5: 提交**

```bash
git add DMT/DMT/Modules/Cursor DMT/DMT/Resources
git commit -m "i18n: resource-ize Cursor module"
```

---

### Task 12: 资源化 Launcher 模块

**Files:**
- Modify: `DMT\DMT\Modules\Launcher\LauncherModule.cs`（5 处节点标签）
- Modify: `DMT\DMT\Modules\Launcher\{LauncherRootOptionsPanel,LauncherMagicWordsOptionsPanel,LauncherHotKeysOptionsPanel,LauncheGeneralOptionsPanel,LauncherImportOptionsPanel,EntryForm,MagicWordForm,ParameterInputForm,InternalCommandForm,StartupPositionControl}.Designer.cs`（约 66 处 `.Text` + 19 处 `.HeaderText` + 10 处 `SetToolTip`）
- Modify: `DMT\DMT\Resources\LauncherStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`

**Interfaces:**
- Consumes: `LauncherStrings`
- Produces: 新增 `LauncherStrings` 键（三种命名：`_Text` / `_HeaderText` / `_ToolTip`）

- [ ] **Step 1: 5 处节点标签**

`"Launcher"`（`启动器`）、`"Magic Words"`（`魔术字`）、`"HotKeys"`（`热键`）、`"General"`（`常规`）、`"Import / Export"`（`导入 / 导出`）。

- [ ] **Step 2: 资源化 `.Text` / `.HeaderText` / `SetToolTip`**

列头用 `<面板名>_<列名>_HeaderText`；工具提示用 `<面板名>_<控件名>_ToolTip`。`ScrollableHotKeysPanel` 在 `Library\HotKeys\` 下，**不属本任务**，由 Task 16 处理。

- [ ] **Step 3: 补 `LauncherStrings` 三条同步**

- [ ] **Step 4: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `Failed: 0`。

- [ ] **Step 5: 提交**

```bash
git add DMT/DMT/Modules/Launcher DMT/DMT/Resources
git commit -m "i18n: resource-ize Launcher module"
```

---

### Task 13: 资源化 SwapScreen 模块

**Files:**
- Modify: `DMT\DMT\Modules\SwapScreen\SwapScreenModule.cs`（5 处节点标签）
- Modify: `DMT\DMT\Modules\SwapScreen\{SwapScreenRootOptionsPanel,SwapScreenActiveOptionsPanel,SwapScreenSdaOptionsPanel,SwapScreenUdaOptionsPanel,SwapScreenOtherOptionsPanel,UdaForm,UdaPanel,ScrollableUdasPanel}.Designer.cs`（约 24 处 `.Text` + 5 处 `SetToolTip`）
- Modify: `DMT\DMT\Resources\SwapScreenStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`

**Interfaces:**
- Consumes: `SwapScreenStrings`
- Produces: 新增 `SwapScreenStrings` 键

- [ ] **Step 1: 5 处节点标签**

`"Swap Screen"`（`交换屏幕`）、`"Active Window"`（`活动窗口`）、`"System Defined Areas"`（`系统定义区域`）、`"User Defined Areas"`（`用户定义区域`）、`"Other Windows"`（`其他窗口`）。

- [ ] **Step 2–4: 资源化 → 补三条同步 → 构建测试**

Run: `.\build.ps1 -Configuration Release -Test`，Expected: `Failed: 0`。

- [ ] **Step 5: 提交**

```bash
git add DMT/DMT/Modules/SwapScreen DMT/DMT/Resources
git commit -m "i18n: resource-ize SwapScreen module"
```

---

### Task 14: 资源化 WallpaperChanger 模块（含 7 个壁纸源插件）

**Files:**
- Modify: `DMT\DMT\Modules\WallpaperChanger\WallpaperChangerModule.cs`（4 处节点标签）
- Modify: `DMT\DMT\Modules\WallpaperChanger\{WallpaperChangerRootOptionsPanel,WallpaperChangerGeneralOptionsPanel,WallpaperChangerPropertiesOptionsPanel,WallpaperChangerProvidersOptionsPanel,ProviderForm}.Designer.cs`（约 36 处 `.Text` + 7 处 `.HeaderText` + 1 处 `SetToolTip`）
- Modify: `DMT\DMT\Modules\WallpaperChanger\Plugins\{Bing,Flickr,Unsplash,List,LocalDisk,RandomShapes,Url}\*.Designer.cs`（约 5 个窗体的 `.Text`）
- Modify: `DMT\DMT\Resources\WallpaperStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`

**Interfaces:**
- Consumes: `WallpaperStrings`
- Produces: 新增 `WallpaperStrings` 键

- [ ] **Step 1: 4 处节点标签**

`"Wallpaper Changer"`（`壁纸切换器`）、`"General"`（`常规`）、`"Properties"`（`属性`）、`"Providers"`（`提供程序`）。

- [ ] **Step 2: 资源化 5 个主面板 + 插件窗体**

插件窗体的键名加插件前缀避免冲突。**注意两个同名类**：`Plugins\Unsplash\WebScrapeForm` 与 `Plugins\Url\WebScrapeForm`，它们命名空间不同（`DMT.Modules.WallpaperChanger.Plugins.Unsplash` / `.Url`）但类名相同，键名必须用 `UnsplashWebScrapeForm_*` 与 `UrlWebScrapeForm_*` 两种前缀区分，否则 resx 键会互相覆盖。

同理 `Plugins\List\ListAutoGenForm` 与 `ListForm` 归本任务，键名用 `ListAutoGenForm_*` / `ListForm_*`。

全部归入 `WallpaperStrings`（spec 3.1 明确「含 7 个插件」）。

- [ ] **Step 3–5: 补三条同步 → 构建测试 → 提交**

Run: `.\build.ps1 -Configuration Release -Test`，Expected: `Failed: 0`。

```bash
git add DMT/DMT/Modules/WallpaperChanger DMT/DMT/Resources
git commit -m "i18n: resource-ize WallpaperChanger module and its plugins"
```

---

### Task 15: 资源化 Snap 模块

**Files:**
- Modify: `DMT\DMT\Modules\Snap\SnapModule.cs`（2 处节点标签）
- Modify: `DMT\DMT\Modules\Snap\{SnapRootOptionsPanel,SnapGeneralOptionsPanel,SnapForm}.Designer.cs`（约 21 处 `.Text`）
- Modify: `DMT\DMT\Resources\SnapStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`

**Interfaces:**
- Consumes: `SnapStrings`
- Produces: 新增 `SnapStrings` 键

- [ ] **Step 1: 2 处节点标签**

`"Snap"`（`截图`）、`"General"`（`常规`）。

- [ ] **Step 2–5: 资源化 → 补三条同步 → 构建测试 → 提交**

Run: `.\build.ps1 -Configuration Release -Test`，Expected: `Failed: 0`。

```bash
git add DMT/DMT/Modules/Snap DMT/DMT/Resources
git commit -m "i18n: resource-ize Snap module"
```

---

### Task 16: 资源化 GuiUtils 与 HotKeys

**Files:**
- Modify: `DMT\DMT\Library\GuiUtils\EditNameListForm.Designer.cs`（约 5 处 `.Text`）
- Modify: `DMT\DMT\Library\HotKeys\{HotKeyForm,HotKeyPanel,KeyComboPanel,ScrollableHotKeysPanel}.Designer.cs`（约 14 处 `.Text`）
- Modify: `DMT\DMT\Resources\CommonStrings.resx` + `.Designer.cs` + `.zh-Hans.resx`

**Interfaces:**
- Consumes: `CommonStrings`（spec 3.1：`Library\GuiUtils\*` 与 `Library\HotKeys\*` 归 `CommonStrings`）
- Produces: 新增 `CommonStrings` 键

- [ ] **Step 1: 确认归属**

四个文件均已实测位于 `DMT\DMT\Library\GuiUtils\` 与 `DMT\DMT\Library\HotKeys\`，无跨模块歧义。`EditNameListForm` 不在 Launcher 下，Task 12 不涉及它。

- [ ] **Step 2–4: 资源化 → 补三条同步 → 构建测试**

键名用 `EditNameListForm_buttonOK_Text`、`HotKeyForm_labelHotKey_Text` 等。中文值注意术语表：HotKey → 热键。

Run: `.\build.ps1 -Configuration Release -Test`，Expected: `Failed: 0`。

- [ ] **Step 5: 提交**

```bash
git add DMT/DMT/Library/GuiUtils DMT/DMT/Library/HotKeys DMT/DMT/Resources
git commit -m "i18n: resource-ize shared GUI and hotkey controls"
```

---

### Task 17: 资源化 DualWallpaper 与 DmtWallpaper 的 Designer

**Files:**
- Modify: `DualWallpaper\DualWallpaper\{AboutForm,DualWallpaper}.Designer.cs`（28 处 `.Text` + 12 处 `SetToolTip`）
- Modify: `DualWallpaper\DualWallpaper\Properties\Resources.resx` + `.Designer.cs` + `.zh-Hans.resx`
- Modify: `DmtWallpaper\DmtWallpaper\{DmtWallpaperForm,ImageBlender,SettingsForm}.Designer.cs`（10 处 `.Text`）
- Modify: `DmtWallpaper\DmtWallpaper\Properties\Resources.resx` + `.Designer.cs` + `.zh-Hans.resx`

**Interfaces:**
- Consumes: 两个工程的 `Properties.Resources`（Task 7 / Task 8 已建 zh-Hans 卫星）
- Produces: 新增 `Properties.Resources` 键

- [ ] **Step 1: DualWallpaper 资源化**

28 处 `.Text` + 12 处 `SetToolTip` 改为 `Resources.<窗体名>_<控件名>_Text` / `_ToolTip`。该工程现有 10 条语义键（`AboutMenuItem` 等）保留不动，新键并存。

注意 `DualWallpaper.Designer.cs` 有单行拼接字面量，先合并成一条再资源化。

- [ ] **Step 2: DmtWallpaper 资源化**

10 处 `.Text` 同理。该窗体是屏保，运行在独立桌面，但资源查找与语言应用不依赖桌面。

- [ ] **Step 3: 两个工程各补 resx 三条同步**

`Properties\Resources.resx` 加英文 `<data>`；`Properties\Resources.Designer.cs` 手写属性；`Properties\Resources.zh-Hans.resx` 加中文。

- [ ] **Step 4: 构建 + 测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `全部构建成功。`，`Failed: 0`。

- [ ] **Step 5: 提交**

```bash
git add DualWallpaper DmtWallpaper
git commit -m "i18n: resource-ize DualWallpaper and DmtWallpaper designers"
```

---

### Task 18: 开启全局字面量守卫测试

**Files:**
- Modify: `DMT\DMT.UnitTests\Library\Localization\ResourceCoverage_Test.cs`

**Interfaces:**
- Consumes: Task 9–17 已完成的全部资源化
- Produces: `NoLiteralTextRemainsInDmtDesigners` 开启并全绿

- [ ] **Step 1: 启用 `NoLiteralTextRemainsInDmtDesigners`**

扫描范围：`DMT\DMT` 下所有 `*.Designer.cs`，排除 `\Resources\` 与 `\Properties\` 两个目录（那些是资源类自身，不是 UI）。

匹配 `\.Text\s*=\s*"[^"]*"` 与 `\.HeaderText\s*=\s*"[^"]*"` 与 `SetToolTip\s*\([^,]+,\s*"`。

白名单（仅这些允许保留字面量）：

- `"DMT_GUI_WINDOW"`（Review Focus 1）
- 形如 `"label<N>"` 的设计期占位符。实测共 6 处：`GeneralMonitorNamesOptionsPanel.labelBrand/labelGpu/labelSize`、`GeneralOptionsPanel.labelInstallType/labelVersion`、`ParameterInputForm.labelPrompt`
- `"-"`（菜单分隔符）
- 字体名字符串（`"Microsoft Sans Serif"` / `"Segoe UI"`，位于 `new Font(...)` 而非 `.Text`）

失败时把违规的 `文件名:行号:内容` 全部列出。

- [ ] **Step 2: 同样扫 DualWallpaper 与 DmtWallpaper**

参数化 `[TestCase(@"DualWallpaper\DualWallpaper")]` / `[TestCase(@"DmtWallpaper\DmtWallpaper")]`。

- [ ] **Step 3: 运行测试**

Run: `.\build.ps1 -Configuration Release -Test`
Expected: `Failed: 0`。若有违规，回对应任务补完。

- [ ] **Step 4: 提交**

```bash
git add DMT/DMT.UnitTests/Library/Localization
git commit -m "test(i18n): assert no user-visible literals remain in designers"
```

---

### Task 19: 文档与最终验证

**Files:**
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: 全部前置任务
- Produces: 无

- [ ] **Step 1: `AGENTS.md` 第 5.2 节补 `Language` 值**

在该节表格的「壁纸路径」行之后加一行：

| 界面语言 | `HKCU\SOFTWARE\Dual Monitor Tools\DMT` → `Language`（REG_SZ，`en` / `zh-Hans`） | `Library\Environment\DmtRegistry.cs` 的 `SetLanguage`，触发于语言选项面板 | 与 `DmtWallpaperPath` 同子树，整棵子树可删 |

- [ ] **Step 2: `AGENTS.md` 补「不换字体」的决定**

在第 7 节「已知坑」之后或第 5 节末尾加一小段，记录实测依据：雅黑 9pt 会让 29 个 Form 高度涨 42%（OptionsForm 767×487 → 895×690）；中文在系统默认字体下靠 `FontLink\SystemLink` 回退到 SimSun/雅黑可正常渲染（位图指纹法验证）；因此不新增任何字体覆盖。

- [ ] **Step 3: `AGENTS.md` 第 7 节补构建产物**

说明 `bin\Release\zh-Hans\<程序名>.resources.dll` 是新增产物，属必需件，分发时不能漏。

- [ ] **Step 4: 从零全量复验**

Run:
```powershell
cd D:\workapsce\common\DMT
.\build.ps1 -Clean
.\build.ps1 -Configuration Release -Test
```
Expected: `全部构建成功。`；NUnit `Failed: 0`；用例总数 ≥ 148 + 新增（记录实际数字）。

- [ ] **Step 5: 验证三个卫星程序集都在**

Run:
```powershell
Get-ChildItem -Recurse -Filter '*.resources.dll' -Path 'DMT\DMT\bin','DualWallpaper\DualWallpaper\bin','DmtWallpaper\DmtWallpaper\bin' |
  ForEach-Object { $_.FullName }
```
Expected: 三条路径，且都在 `zh-Hans\` 子目录下。

- [ ] **Step 6: 命令行冒烟**

Run: `.\DMT\DMT\bin\Release\DMT.exe -v`
Expected: 打印 `DMT 2.13.0.0`。不启动 GUI。

- [ ] **Step 7: 提交**

```bash
git add AGENTS.md
git commit -m "docs: record Language registry value and the no-font-change decision"
```

---

## 完成标准

全部任务勾选完毕后应满足：

1. `.\build.ps1 -Configuration Release -Test` 从 clean 状态可重现成功，`Failed: 0`。
2. 三个程序各生成 `zh-Hans\<程序名>.resources.dll`。
3. `DMT.exe -v` 正常输出，未产生任何 GUI 副作用。
4. `HKCU\SOFTWARE\Dual Monitor Tools\DMT` 下 `Language` 值可被语言面板写入（由 `DmtRegistry.SetLanguage` 实现，未跑 GUI 故未实测写入，属未验证项）。
5. 中英文界面观感**未经目视验证**（受 `AGENTS.md` 第 4.4 条约束），需用户真机确认。
