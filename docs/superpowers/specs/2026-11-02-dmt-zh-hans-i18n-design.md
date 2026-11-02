# DMT 简体中文 i18n 与语言选择设计

- 日期：2026-11-02
- 状态：待评审
- 涉及工程：`DMT\DMT\DMT.csproj`、`DualWallpaper\DualWallpaper\DualWallpaper.csproj`、`DmtWallpaper\DmtWallpaper\DmtWallpaper.csproj`

## 1. 目标与范围

为 Dual Monitor Tools 2.13 增加运行期界面语言选择，并提供一份覆盖三个程序的简体中文翻译。

### 1.1 用户故事

1. 用户打开 Options 对话框，在左侧树 `Dual Monitor Tools` 分支下找到 `Language` 叶子节点，点击后在右侧看到语言列表。
2. 用户从列表中选中「简体中文」，设置立即写入注册表。
3. 用户关闭并重启 DMT，全部界面（主程序、Options 树、各模块面板、对话框、错误提示）显示为简体中文。
4. 用户随时可以选回 `English`，重启后恢复英文。

### 1.2 需求边界

| 项 | 决定 |
|---|---|
| 语言数量 | 首批 `English`（en）+ `简体中文`（zh-Hans）两种 |
| 扩展性 | 新增语言只需加卫星 `.resx` 与在 `LanguageCatalog` 加一行，不改逻辑代码 |
| 入口位置 | **只**在 Options 对话框的树里加节点，**不**改托盘右键菜单 |
| 生效时机 | 重启程序后生效；设置页不尝试热切换 |
| 默认语言 | 用户未选择过时一律 `English`，不跟随系统区域 |
| 翻译覆盖 | 三个可执行程序的全部可见界面文案 |
| 字体 | **不改**，保持系统默认（`Control.DefaultFont`），中文靠系统 FontLink 回退渲染 |
| 布局 | **不改**，字体不变即无缩放差异；实测中文比英文更窄 |

### 1.3 明确不做

- 不启动 GUI 做目视验证（`AGENTS.md` 第 4 节第 4 条）。界面观感由用户在真机确认。
- 不热切换语言（不重建已创建窗体）。
- 不做语言包的运行时外部加载（不发 JSON 语言包）。
- 不翻译日志文本、内部命令名（`DMT:General:Options` 这类协议串）、注册表键名、`DmtSettings.xml` 的 `name` 属性。
- 不翻译壁纸源插件在 `DmtWallpaperProviders.xml` 里已保存的 `description` 值（用户数据，只翻译新建时的默认值）。
- 不升级目标框架，不引入任何第三方 i18n 库。

## 2. 语言状态的存储与共享

### 2.1 存储位置

语言选择存入注册表：

```
HKCU\SOFTWARE\Dual Monitor Tools\DMT   值名 Language   REG_SZ   内容 "en" 或 "zh-Hans"
```

选注册表而非 `DmtSettings.xml` 的理由：三个程序都要读这个值，而 `DmtWallpaper.scr` 不引用 `DMT.Library.Settings` 的任何文件（它只用 `WallpaperFilenameHelper` 直接读同一个键）。走注册表可让三个程序共用一条最小通道，不必给屏保引入整套 `FileLocations` + XML 读取。

这条键与已有的 `DmtWallpaperPath` 同处一棵子树，卸载时归入 `AGENTS.md` 第 5.2 节「`HKCU\SOFTWARE\Dual Monitor Tools` 整棵子树可删」，卸载顺序无需变更。`AGENTS.md` 第 5.2 节需补一行说明新增值。

缺失或无法识别时的行为：视为 `en`。读取异常（无权限等）不抛异常、不弹窗，静默回退 `en`。

**必须显式设置 `Culture`，不能依赖「不设置」。** 已用独立 spike 工程实测（.NET Framework 4.0 + Client Profile，真实生成卫星程序集）：

| 资源类 `Culture` 取值 | 实际取到的字符串 |
|---|---|
| `null`（不设置） | **`你好`** |
| `en` | `Hello` |
| `zh-Hans` | `你好` |
| `zh-CN` | `你好`（沿 `zh-CN → zh-Hans` 父链回退到卫星程序集） |
| `InvariantCulture` | `Hello` |

第一行是本设计的一个陷阱：`Culture` 为 `null` 时 `ResourceManager` 退回线程的 `CurrentUICulture`，而本机是 `zh-CN`，因此**什么都不做会直接得到中文**，与「默认英文、不跟随系统区域」的需求相反。所以 `UiCulture.Apply` 必须在启动时无条件把每个资源类的 `Culture` 设成选定值（默认 `en`），不能只在用户选过语言时才设。

同一实测也确认了 `zh-CN` 能沿父链命中 `zh-Hans` 卫星程序集——这是选用 `zh-Hans` 作为标签的直接好处，中文系统用户即使区域是 `zh-CN`/`zh-TW` 也能正确回退。

### 2.2 三个程序的接入方式

| 程序 | 接入方式 |
|---|---|
| `DMT.exe` | 在 `DMT\DMT\Library\Environment\DmtRegistry.cs` 增加 `GetLanguage()` / `SetLanguage(string)` |
| `DualWallpaper.exe` | csproj 已把 `DmtRegistry.cs` 作为共享源文件编入（`<Compile Include="..\..\DMT\DMT\Library\Environment\DmtRegistry.cs">`），无需改动工程文件 |
| `DmtWallpaper.exe/.scr` | csproj 新增一行 `<Compile Include="..\..\DMT\DMT\Library\Environment\DmtRegistry.cs">`，与 `DualWallpaper` 同样方式复用，不复制代码 |

`DmtRegistry` 现有代码把键名与值名写成 `const` 私有字段，新增 `LanguageValueName = "Language"` 常量，保持同风格。

### 2.3 语言目录

`LanguageCatalog.cs` 只依赖 `DmtRegistry`，无其他依赖，**三个工程都通过 `Compile Include` 共享这一个文件**，不复制代码、不各自维护语言列表：

```xml
<Compile Include="..\..\DMT\DMT\Library\Localization\LanguageCatalog.cs">
```

`DualWallpaper.csproj` 与 `DmtWallpaper.csproj` 各自加这一行（连同 2.2 节的 `DmtRegistry.cs`）。三个程序因此共享同一份语言清单，新增语言只改一处。

```csharp
namespace DMT.Library.Localization
{
	/// <summary>
	/// One selectable UI language.
	/// </summary>
	public class LanguageDefinition
	{
		/// <summary>Culture name stored in the registry, e.g. "en" or "zh-Hans".</summary>
		public string Tag { get; private set; }

		/// <summary>Name shown in the UI, written in the language's own script.</summary>
		public string DisplayName { get; private set; }

		public LanguageDefinition(string tag, string displayName) { ... }
	}

	/// <summary>
	/// The set of languages this build can display.
	/// Add a language by adding its satellite .resx files and one entry below.
	/// </summary>
	public static class LanguageCatalog
	{
		/// <summary>All supported languages, default first.</summary>
		public static ReadOnlyCollection<LanguageDefinition> All { get; }

		/// <summary>The language used when no valid preference is stored.</summary>
		public static LanguageDefinition Default { get; }

		/// <summary>Finds a language by tag; returns Default when unknown.</summary>
		public static LanguageDefinition Find(string tag);

		/// <summary>Returns the CultureInfo for a tag, falling back to Default.</summary>
		public static CultureInfo CultureFor(string tag);

		/// <summary>Reads the saved preference; returns Default.Tag when absent or invalid.</summary>
		public static string ResolveFromRegistry();
	}
}
```

**类型选择受目标框架限制**：`IReadOnlyList<T>` 在 .NET Framework 4.0 不存在（4.5 才引入），已用独立 spike 工程实测确认——用它会报 `CS0246: 未能找到类型或命名空间名"IReadOnlyList<>"`。因此对外暴露 `ReadOnlyCollection<LanguageDefinition>`（`System.Collections.ObjectModel`，4.0 可用，已实测编译通过）。同理不使用 `?.`、`nameof`、字符串内插、表达式体成员等 C# 6+ 语法，代码风格保持 C# 5 以内。

`DisplayName` 用各自语言写自己，切换界面语言后语言名不变，也不需要翻译条目。

**扩展方式**：加一种语言 = 每个资源类加一份 `<类名>.<tag>.resx` + `LanguageCatalog.All` 加一行。逻辑代码零改动。

### 2.4 语言生效机制

语言必须在**任何窗体创建之前**应用。三个程序各自在 `Main` 的最早位置调用一次：

```csharp
UiCulture.Apply(LanguageCatalog.ResolveFromRegistry());
```

新增 `DMT\DMT\Library\Localization\UiCulture.cs`，**同样由三个工程 `Compile Include` 共享**：

```xml
<Compile Include="..\..\DMT\DMT\Library\Localization\UiCulture.cs">
```

`Apply(string tag)` 做三件事：

1. 构造 `CultureInfo`（`"en"` 用 `CultureInfo.InvariantCulture` 会丢英文资源，故用 `new CultureInfo("en")`）。
2. 设 `Thread.CurrentThread.CurrentUICulture = culture`。
3. 用反射把 `DMT.Resources.*Strings` 与 `Properties.Resources` 等资源类的静态 `Culture` 属性统一设为 `culture`。

第 3 步是必需的：目标框架是 .NET Framework 4.0，没有 `CultureInfo.DefaultThreadCurrentUICulture`（4.5 才引入），而 DMT 内部会在非 UI 线程上读资源（后台壁纸切换线程）。逐个显式设资源类的 `Culture` 可保证无论哪个线程读、读到哪种语言都一致。反射遍历调用方程序集里所有含静态 `Culture` 属性的类型，不硬编码类名清单。

`UiCulture.Apply` 不涉及字体——本设计不改字体（见第 5 节）。

### 2.5 语言面板与重启提示

`Modules\General\LanguageOptionsPanel.cs` + `.Designer.cs`（`UserControl`，与 `GeneralRootOptionsPanel` 同构）：

- 一个 `ListBox` 列出 `LanguageCatalog.All`，显示 `DisplayName`。
- 一段说明文字：「界面语言」。下方小字：「更改语言后需要重启 Dual Monitor Tools 才能完全生效。」
- 选中某项即写注册表，无「应用/取消」按钮——与项目已有 `StringSetting` 的「改了就存」行为一致。
- 若当前选中的语言与启动时使用的语言不同，说明文字切换为：「语言已保存。请重启 Dual Monitor Tools 以应用更改。」

`GeneralModule.GetOptionNodes()` 在 `Monitors` 叶子之后追加：

```csharp
_languageOptionsPanel = new LanguageOptionsPanel();
options.Nodes.Add(new ModuleOptionNodeLeaf(GeneralStrings.Node_Language, image, _languageOptionsPanel));
```

无 `Monitors` 叶子时（非 Win7+）仍追加在 `Monitor Order` 之后，保证节点总存在。

## 3. 资源组织

### 3.1 DMT.exe：扩展已有 8 个资源类

不新建集中资源类，按窗体归属分派：

| 区域 | 资源类 |
|---|---|
| `AppForm` / `OptionsForm` / `AboutForm` | `AppStrings` |
| `Library\GuiUtils\*` / `Library\HotKeys\*` | `CommonStrings` |
| `Modules\Cursor\*` | `CursorStrings` |
| `Modules\General\*`（含新增 `LanguageOptionsPanel`） | `GeneralStrings` |
| `Modules\Launcher\*` | `LauncherStrings` |
| `Modules\Snap\*` | `SnapStrings` |
| `Modules\SwapScreen\*` | `SwapScreenStrings` |
| `Modules\WallpaperChanger\*`（含 7 个插件） | `WallpaperStrings` |

### 3.2 键命名

新增键采用机械可追溯格式，避免与现有 154 条语义键冲突：

| 用途 | 格式 | 例子 |
|---|---|---|
| 控件文字 | `<窗体名>_<控件名>_Text` | `LocalDiskForm_buttonBrowse_Text` |
| 列头 | `<窗体名>_<控件名>_HeaderText` | `ListForm_columnPath_HeaderText` |
| 工具提示 | `<窗体名>_<控件名>_ToolTip` | `SnapForm_buttonSnap_ToolTip` |
| 选项树节点 | `Node_<英文原名>` | `Node_Language`、`Node_MonitorNames` |
| 非控件文案（错误、格式串） | 沿用现有语义名 | `CommonStrings.FileExists` |

改后的 Designer 行形如：

```csharp
this.buttonClose.Text = AppStrings.OptionsForm_buttonClose_Text;
```

现有 154 条键的语义名与内容不改，只补一份 `.zh-Hans.resx`。

### 3.3 DualWallpaper 与 DmtWallpaper

**两个小工程复用已有的 `Properties.Resources`，不新建资源类。**

实测三个工程的 `Properties\Resources.resx` 内容差异很大，处理方式因此不同：

| 工程 | `Properties\Resources.resx` 内容 | 处理 |
|---|---|---|
| DMT | 35 条全部是 `ResXFileRef` 二进制引用（图标/位图：`dual_48_24.png`、`general.ico`、`NumPad1.png` 等），**无任何字符串** | 不加 zh-Hans；UI 文案全部走 3.1 节的 8 个 `XxxStrings` |
| DualWallpaper | 10 条字符串（`AboutMenuItem`、`Center`、`License`、`MyTitle`、`OpenImageFilter`、`OverStretch`、`SaveImageFilter`、`Stretch`、`UnderStretch`、`WebsiteMenuItem`），**均面向用户** | 直接在此 resx 补新键 + 加 `Properties\Resources.zh-Hans.resx` |
| DmtWallpaper | 2 条字符串（`MyTitle`、`License`）+ 2 条 PNG 引用 | 同上 |

理由：这两个工程的 `Properties.Resources` 已经是「用户可见字符串 + 图标」的混合容器，其 `.Designer.cs` 已有 `internal static CultureInfo Culture` 属性（已核实），卫星程序集机制可直接套用。再建一个 `UiStrings` 只会让「哪条字符串在哪个类」变得难以判断，属于多余抽象。

因此**不新增** `UiStrings.resx` / `UiStrings.Designer.cs`。新增键沿用 3.2 的 `<窗体名>_<控件名>_Text` 格式，与既有 10 条语义键共存。

注意：`Resources.zh-Hans.resx` 只放翻译后的**字符串**键，不复制 `ResXFileRef` 二进制条目；缺图时 CLR 自动回退中性 resx 的图标引用。

### 3.4 卫星资源文件

三个工程各为每个资源类提供 `zh-Hans` 版本：

```
DMT\DMT\Resources\CommonStrings.zh-Hans.resx
DMT\DMT\Resources\CursorStrings.zh-Hans.resx
... （DMT 共 8 个，与中性 resx 同目录）
DualWallpaper\DualWallpaper\Properties\Resources.zh-Hans.resx
DmtWallpaper\DmtWallpaper\Properties\Resources.zh-Hans.resx
```

构建产出 `<Cfg>\zh-Hans\<程序集名>.resources.dll`。

**已实测确认的 csproj 改法**（在 `DMT.csproj` 上做过端到端验证，测完已还原）：

```xml
<EmbeddedResource Include="Resources\GeneralStrings.resx">
  <Generator>ResXFileCodeGenerator</Generator>
  <LastGenOutput>GeneralStrings.Designer.cs</LastGenOutput>
</EmbeddedResource>
<EmbeddedResource Include="Resources\GeneralStrings.zh-Hans.resx">   <!-- 新增 -->
  <DependentUpon>GeneralStrings.resx</DependentUpon>
</EmbeddedResource>
```

只需新增 `EmbeddedResource` 一行组，`<DependentUpon>` 指向中性 resx。构建日志显示 MSBuild 自动调用 `al.exe /culture:zh-Hans` 生成卫星程序集，`bin\Release\zh-Hans\DMT.resources.dll` 为 4608 字节。

运行时验证：`ResourceManager("DMT.Resources.GeneralStrings", DMT.exe)` 在 `zh-Hans` 与 `zh-CN` 下都返回测试中文值，`en` 下返回英文原文。

**不需要 `<NeutralLanguage>`。** 我原先以为必须加，实测否证：把 `NeutralLanguage` 去掉、清空 `obj` 与 `bin` 重新构建，`al.exe` 同样被调用、卫星程序集同样生成、`zh-CN` 同样正确回退。因此**不加这个属性**，保持 csproj 改动最小。

`zh-Hans` 卫星 resx 中**只放已翻译的键**；缺键时 CLR 自动回退到中性 resx 的英文值，这是 ResX 的标准行为，不需要补齐占位。

### 3.5 资源键实际数量

用 XML 解析（而非文本 grep）统计 8 个中性 resx 的真实 `<data>` 条目：

| 资源类 | 键数 |
|---|---|
| `AppStrings` | 2 |
| `CommonStrings` | 24 |
| `CursorStrings` | 14 |
| `GeneralStrings` | 12 |
| `LauncherStrings` | 18 |
| `SnapStrings` | 9 |
| `SwapScreenStrings` | 39 |
| `WallpaperStrings` | 24 |
| **合计** | **142** |

（`DmtWallpaper` + `DualWallpaper` 的 `Properties.Resources` 另有 2 + 10 条字符串键。全项目需翻译的既有键共 **154** 条，尚未计入第 4 节新增的 Designer 文案键。）

**统计陷阱**：每个 resx 的第 3–61 行是 Microsoft ResX Schema 的说明性 XML 注释块，其中示范了 `Name1`、`Color1`、`Bitmap1`、`Icon1` 四个 `<data>` 条目。用文本正则 `grep '<data name="'` 统计会把它们算进去（得出虚高的 174），但这些**不是真实资源**，XML 解析后不会出现。翻译与覆盖率测试都必须用 XML 解析，不用文本匹配。

## 4. Designer 文案资源化

### 4.1 范围

| 工程 | Designer 文件 | `.Text` 字面量 | `.HeaderText` | `SetToolTip(` |
|---|---|---|---|---|
| DMT | 67 | 388 | 26 | 18 |
| DualWallpaper | 4 | 28 | 0 | 12 |
| DmtWallpaper | 5 | 10 | 0 | 0 |

非 Designer 代码里的另外两类用户可见文案（已核实）：

- **选项树节点标签 26 处**，形如 `new ModuleOptionNodeLeaf("General", image, ...)`，分布在 `CursorModule.cs`（5）、`GeneralModule.cs`（5）、`LauncherModule.cs`（5）、`SnapModule.cs`（2）、`SwapScreenModule.cs`（5）、`WallpaperChangerModule.cs`（4）。
- **`MessageBox.Show` 共 14 处活动调用**（另有 8 处被注释掉），但**第一参数没有一处是字面量**——全部是变量（`ex.Message`、`msg`）或 `CommonStrings.MyTitle`。所以这类调用不需要改。`SetToolTip` 在非 Designer 代码里也是 0 处字面量调用。
- `AppForm.cs` 构造函数里 4 处 `AddMenuItem("About", ...)` / `("Visit Website", ...)` / `("-", ...)` / `("Exit", ...)`。其中 `"-"` 是分隔符，不翻译。

### 4.2 做法

逐个把 `*.Designer.cs` 中的字符串字面量替换为资源类引用，并在对应 resx 补键。规则：

- 只替换**面向用户的可见文案**。
- 不替换：控件 `Name`、`Tag`、代码里的标识符、字体名字符串、MIME/格式串、`AccessibleName` 之外的内部值。
- 单行拼接的字面量（如 `DualWallpaper.Designer.cs` 的 `label4.Text = "..." + "..."`）先合并为一条资源。若拼接两段语义不同，拆成两条资源并用 `string.Format` 或 `+` 组合。
- `labelInstallType.Text = "label9"`、`labelVersion.Text = "label7"` 这类是设计期占位符，运行时会被代码覆盖（`GeneralModule` 里赋实际值），**保留原样不改**。
- 赋值顺序不变、不改 `Location`/`Size`（布局规则见第 5 节）。

### 4.3 运行时创建的控件

`AppForm` 构造函数里的 4 处 `AddMenuItem("About", null, ...)` 等，改为传入 `AppStrings` 的对应键。这类改动在 `.cs` 而非 `.Designer.cs`。

### 4.4 不改 `Localizable`

全部窗体保持 `Localizable=false`。理由：改成 `true` 会让 WinForms 把布局也搬进各语言 resx，等于引入一套并行的布局机制，远超本次需求（文案翻译）的范围。

## 5. 字体与布局

### 5.1 字体：不做任何改动

**本设计不设置任何字体覆盖，保持运行时的系统默认字体。**

经实测验证（Windows PowerShell 5.1 下真实构造 `Form` / `UserControl`，96 DPI）：

| 方案 | OptionsForm 实际 `ClientSize` | 相对今天 |
|---|---|---|
| **今天（不设字体，`Control.DefaultFont` = SimSun 9pt）** | **767 × 487** | **基准，零变化** |
| Microsoft Sans Serif 9pt（设计器假定的基准族） | 895 × 609 | 宽 +17%、高 +25% |
| Microsoft YaHei 9pt | 895 × 690 | 宽 +17%、高 +42% |
| Microsoft YaHei 7.5pt | 767 × 649 | 宽 0%、高 +33% |

三条决定性事实：

1. **中文不需要换字体也能正常显示。** 位图指纹法验证：`Microsoft Sans Serif` 9pt 下「常规設置顯示」六字渲染出六个互不相同的字形，不是缺字方框。系统 `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontLink\SystemLink` 中 `Microsoft Sans Serif` 已配置 `SIMSUN.TTC,SimSun` 与 `MSYH.TTC,Microsoft YaHei UI` 回退链，CJK 走 fallback 正常绘制。`Control.DefaultFont` 在本机（zh-CN）为 `SimSun 9pt`，本身即中文字体。
2. **不设字体时布局完全不变**，实测 `ClientSize` 精确保持 767×487。
3. **`AutoScaleMode=Font` 的缩放是框架行为，无法「只换字体不动布局」。** 唯一能让缩放因子等于 1.0 的办法是把 56 处 `AutoScaleDimensions` 从 `6F, 13F` 改成目标字体的实际度量，但那是谎报设计基准：设计器布局本来就按 6×13 排的，改了会误导后续维护，且在英文 Windows 上又会反向错位。**不做。**

因此：

- 不新增任何 `this.Font =` 赋值，不改 29 个 Form 与 32 个 UserControl。
- 不改 Designer 里已有的 8 处显式 `Font` 赋值（7 处 `Microsoft Sans Serif`、1 处 `Segoe UI`，均为标题类 `Label` 的放大字号）。它们在中文下同样靠 FontLink 回退正常显示，改动无收益。
- `LanguageDefinition` **不提供** `PreferredFontName` 字段。将来若确需按语言切字体，再加此字段与对应应用逻辑，届时按本节实测数据重新评估代价。

### 5.2 布局

**不做任何布局改动。** 字体不变即无缩放差异，`Location` / `Size` / `Anchor` / `AutoSize` 全部保持原样。

关于中文比英文宽的问题：实测同一批真实文案的中文宽度是英文的 **43%–80%**（例：`Change the wallpaper` 127px → `更换壁纸` 55px；`Settings that are common to all of the tools.` 241px → 127px），**中文比英文更窄，不会撑破原有布局**。因此不需要补 `AutoSize`，也不需要位置修正。

### 5.3 字符编码

**实测的现状（必须原样保持）：**

| 文件类型 | 编码 | 换行 |
|---|---|---|
| `.cs` | **UTF-8 with BOM** | CRLF |
| `.csproj` | **UTF-8 with BOM** | CRLF |
| `.resx` | **UTF-8 with BOM** | CRLF |
| `.md`（`AGENTS.md`、本 spec） | UTF-8 无 BOM | LF |
| `.ps1`（含中文） | **UTF-8 with BOM** | CRLF |

已逐个字节验证：`DMT.csproj`、`GeneralStrings.resx`、`GeneralModule.cs`、`AppForm.cs` 首三字节均为 `EF BB BF`，且 LF 数等于 CRLF 数（全 CRLF）。

**这是本次实现最容易踩的坑**：用 `[System.IO.File]::WriteAllText($p, $raw, (New-Object System.Text.UTF8Encoding($false)))` 改写 `.cs` / `.csproj` / `.resx` 会**静默剥掉 BOM**，`git diff` 里会多出一行「文件头变化」。实测中我在改 `DMT.csproj` 时就犯了此错，已修复。改写这三类文件必须用 `UTF8Encoding($true)`。

`.resx` 里的中文字符串直接写 UTF-8 原文，不转义为 `&#x...;` 实体，依赖首行 `<?xml version="1.0" encoding="utf-8"?>` 声明。

新建 `.zh-Hans.resx` 从对应中性 resx 复制后裁剪，保留全部 `<resheader>`，这样文件结构、编码、换行都与既有文件一致。

## 6. 构建与测试

### 6.1 构建

沿用 `build.ps1`，不改脚本逻辑。三个解决方案必须全部 0 error。

`zh-Hans` 卫星程序集是新增产物，`build.ps1` 的产物清单说明需补一行，但不改脚本行为。

### 6.2 验证方式（受 GUI 禁令约束）

| 验证项 | 方式 | 可否自动 |
|---|---|---|
| 编译通过 | `.\build.ps1 -Configuration Release -Test` | 是 |
| 148 个既有单测不回归 | 同上 | 是 |
| 卫星程序集已生成 | 检查 `bin\Release\zh-Hans\DMT.resources.dll` 存在 | 是 |
| 翻译覆盖率 | 新增 NUnit 测试：加载中性 resx 与 `zh-Hans` resx，逐键比对。**断言中文键集覆盖中性键集全部键**（服务「尽可能全覆盖」目标），且中文 resx 无中性 resx 不存在的多余键。未覆盖键以清单形式输出到测试消息，便于逐条补齐 | 是 |
| 中文字符串非空且非英文原文 | 新增 NUnit 测试：断言每条中文值非空、不含未替换的占位符 `{0}`/`{1}` 数量与英文一致 | 是 |
| `Designer.cs` 无残留字面量 | 新增 NUnit 测试：扫描三个工程的 `*.Designer.cs`，断言不存在 `\.Text\s*=\s*"[^"]+"` 形式的面向用户字面量（白名单排除设计期占位符如 `"label9"`） | 是 |
| 语言读取 | 新增 NUnit 测试：`LanguageCatalog.ResolveFromRegistry()` 在值缺失/非法时返回 `en` | 是（不触碰真实注册表，注入假数据） |
| 界面实际观感 | 用户在真机启动 GUI 目视 | **否** |

### 6.3 新增测试的位置

`DMT\DMT.UnitTests\Library\Localization\` 下新增测试文件，遵循既有测试的 NUnit 风格。既有 148 个用例保持全绿。

## 7. 与现有约束的关系

| `AGENTS.md` 约束 | 本设计如何遵守 |
|---|---|
| 第 4.1 条 GPLv3 | 新增文件全部带 GPLv3 版权头；不改 `COPYING.txt` |
| 第 4.2 条 不改原作者代码 | 本设计大量修改 `*.Designer.cs` 与 `*.cs`，**属于用户明确提出的功能需求**（m00611、m00734、m00754、m00821、m00834），符合该条的例外条款 |
| 第 4.3 条 不升级目标框架 | 保持 v4.0 + Client Profile / v4.5；不使用 `CultureInfo.DefaultThreadCurrentUICulture` |
| 第 4.4 条 不启动 GUI | 验证方式见 6.2，全部可自动化的项都自动化，不可自动化的明确标注 |
| 第 4.5 条 不写 HKLM | 只写 `HKCU` |
| 第 5.2 节 卸载清单 | 新增 `Language` 值落在已列入清理范围的 `HKCU\SOFTWARE\Dual Monitor Tools` 子树内，该节需补一行 |
| 第 6 节 代码风格 | Tab 缩进、CRLF、块式 namespace、`using` 在 namespace 内、XML doc 注释、GPLv3 头 |

## 8. 交付物清单

**新增文件**

```
DMT\DMT\Library\Localization\LanguageCatalog.cs
DMT\DMT\Library\Localization\UiCulture.cs
DMT\DMT\Modules\General\LanguageOptionsPanel.cs
DMT\DMT\Modules\General\LanguageOptionsPanel.Designer.cs
DMT\DMT\Modules\General\LanguageOptionsPanel.resx
DMT\DMT\Resources\*.zh-Hans.resx                      （8 个）
DualWallpaper\DualWallpaper\Properties\Resources.zh-Hans.resx
DmtWallpaper\DmtWallpaper\Properties\Resources.zh-Hans.resx
DMT\DMT.UnitTests\Library\Localization\*.cs            （覆盖率测试）
```

**修改文件**

```
DMT\DMT\DMT.csproj                                    新增文件与 zh-Hans EmbeddedResource 收录
DualWallpaper\DualWallpaper\DualWallpaper.csproj      同上
DmtWallpaper\DmtWallpaper\DmtWallpaper.csproj         同上 + 共享 DmtRegistry.cs
DMT\DMT\Library\Environment\DmtRegistry.cs            GetLanguage / SetLanguage
DMT\DMT\Program.cs                                    启动时应用语言
DualWallpaper\DualWallpaper\Program.cs                启动时应用语言
DmtWallpaper\DmtWallpaper\Program.cs                  启动时应用语言
DMT\DMT\Modules\General\GeneralModule.cs              新增 Language 叶节点
DMT\DMT\AppForm.cs                                    4 处菜单项文案资源化
67 个 DMT Designer + 4 个 DualWallpaper Designer + 5 个 DmtWallpaper Designer
对应 8 + 1 + 1 个中性 resx（补键）
AGENTS.md                                             补 Language 值，并记录「不换字体」的实测依据
```

## 9. 风险

| 风险 | 影响 | 缓解 |
|---|---|---|
| 76 个 Designer 文件的机械替换出错 | 编译失败或界面错乱 | 全部靠编译器兜底；替换后逐文件比对差异；单测断言无残留字面量 |
| 中文文案撑破原有控件 | 观感问题 | 实测中文宽度只有英文的 43%–80%，比英文更窄，不会撑破；若个别处仍需微调，限定在单条文案的换行或该控件 `Size` 上 |
| 误以为必须换字体才能显示中文 | 白改并引入 42% 高度膨胀 | 第 5.1 节已用位图指纹法证明原字体经 FontLink 可正常渲染 CJK，spec 明确禁止新增字体覆盖 |
| 反射设资源类 `Culture` 漏掉某类 | 部分界面不翻译 | 单测断言每个资源类的 `Culture` 被正确设置 |
| `.resx` 中文编码写错 | 乱码 | 统一 UTF-8；单测检查关键中文键内容 |
| `zh-Hans` resx 的 `EmbeddedResource` 条目漏写 | 该资源类的中文完全不生效 | 单测检查 `bin\Release\zh-Hans\` 产物存在，并逐资源类比对中文键 |
