# 计划：Dogma 文档站基础设施（DocSite）

> 状态：设计中 / 分支：`feature/code-highlight-displayer`（后续可开新分支）
> 日期：2026-08-09

## 背景与目标

Dogma 将作为不同 Avalonia 控件库的**文档项目基础设施**。每个库（如 Ursa、Semi、
未来的其他库）需要：基于 View/ViewModel 的声明式配置（Attribute），自动获得
**完整的导航与搜索功能**，并满足 **NativeAOT** 发布要求。

## 调研结论（影响架构的事实）

- Ursa / Semi / Avalonia 官方 demo 全部是"手写注册或命名约定反射"（`Ursa.Demo`
  的 ViewLocator 类名约定、`Semi.Avalonia.Demo` 的工厂函数列表），**生态无 Attribute
  扫描方案**，属于自建创新。
- 两库均用 **Source Generator** 做多语言（Irihi.Lingua：resx → 强类型
  `IObservable<string?>` + `Keys` 类 + 编译期键一致性校验 LINGUA002/003）。
  Dogma 作为基础设施**不能绑定任一 i18n 方案**。
- **NativeAOT 约束**决定架构：运行时 `Assembly.GetTypes()` 扫描会被裁剪，
  必须走 **Roslyn Source Generator**（编译期生成注册表，零运行时反射）。

## 已确认决策

| 项 | 决策 |
|---|---|
| 标记位置 | `[DocPage]` 标在 **ViewModel** 上，`View = typeof(...)` 显式指定 View |
| 搜索范围 | 元数据（标题/类别/关键字） + 文档文本（Description） |
| AOT | 必须满足 NativeAOT（SG 生成注册表，无运行时反射扫描） |
| 导航形态 | 树形侧栏（Category > Page，Order 排序，可折叠） |
| 本地化 | 键优先 + `IDocStringProvider` 抽象；支持运行时文化切换；Lingua 适配仅示例代码 |

## 架构：三层

```
┌─ 标记层    [DocPage] Attribute（标在 VM，typeof 指定 View，存资源键）
├─ 生成层    DocPageGenerator（Roslyn 增量生成器，编译期扫描 + resx 键校验 → 静态注册代码）
└─ 运行时层  DocSite 注册表 + 树构建/搜索 + DocShell 控件 + IDocStringProvider
```

## 1. 标记层

```csharp
[DocPage(TitleKey = "Docs.Button.Title",          // 资源键（优先）
         CategoryKey = "Docs.Controls",
         DescriptionKey = "Docs.Button.Desc",
         View = typeof(ButtonView),
         Order = 1,
         Title = "Button",                          // fallback 字符串
         Keywords = new[] { "click", "action" })]   // 搜索关键字
public sealed partial class ButtonViewModel { }
```

| 参数 | 用途 |
|---|---|
| `TitleKey` / `Title` | 导航/搜索标题（键优先，字符串 fallback） |
| `CategoryKey` | 树形侧栏一级分类（同样键优先） |
| `DescriptionKey` | 文档文本，纳入搜索索引 |
| `View` | VM 对应的 View 类型（`typeof` 指定） |
| `Order` | 分类内排序 |
| `Keywords` | 补充搜索关键字（不随文化变） |

## 2. 生成层（AOT 关键 + 多语言校验）

`DocPageGenerator : IIncrementalGenerator`（netstandard2.0，零运行时依赖）：

- 编译期扫描 `[DocPage]`，读取全部参数 → 生成静态注册代码：

```csharp
// GeneratedDocPages.g.cs（自动生成）
public static partial class GeneratedDocPages
{
    public static void Register(IDocRegistry registry) =>
        registry.AddPage(new DocPageMetadata(
            "Docs.Button.Title", "Button",          // 键 + 烘焙的默认文化文本
            typeof(ButtonView), "Docs.Controls",
            1, "描述...", new[] { "click" },
            viewFactory: () => new ButtonView(),
            viewModelFactory: () => new ButtonViewModel()));
}
```

- 类型引用、`new` 调用全部编译期写死 → **NativeAOT 裁剪安全**
- **编译期键校验**：通过 `AdditionalFiles` 读取宿主默认文化 resx，校验
  `TitleKey/CategoryKey/DescriptionKey` 存在，否则报编译错误 `DOGDOC001`
  （体验对齐 Lingua LINGUA002）；同时把默认文化文本**烘焙**为字面量
  （搜索索引 + 无 provider 时 fallback）
- 增量生成：Attribute/resx 增删改自动重新生成

## 3. 运行时层

### DocSite（注册表 + 核心逻辑）

- `AddPage(DocPageMetadata)`（由生成的 `GeneratedDocPages.Register` 调用）
- 树构建：`Category → Page`，按 `Order` 排序
- `Navigate(page)`：用编译期工厂创建 View + 设 DataContext = VM
- `Search(query)`：标题 > 关键字 > 描述，加权子串匹配（OrdinalIgnoreCase），
  索引基于**烘焙默认文本**（跨文化稳定）+ 当前文化文本

### DocShell（控件，复用 CodeBlock 的 ControlTheme 模式）

- 左侧树形侧栏（分类可折叠）
- 搜索框：输入即过滤树 + 空结果提示（参考 Semi 的 FilteredSections 体验）
- 内容区 `ContentControl` 呈现当前页 View

### 宿主接入

```csharp
// App.axaml.cs
GeneratedDocPages.Register(DocSite.Instance);
DocSite.Instance.SetStringProvider(new ResxDocStringProvider(typeof(LanguageManager).Assembly, "Resources.Strings"));
// 用 Lingua 时（示例代码桥接，不建包）：
// DocSite.Instance.SetStringProvider(new LinguaDocStringProvider(LanguageManager.Instance));
```

## 4. 本地化（进化方案）

对比"绑定某 i18n 库"，Dogma 采用**键优先 + 抽象 + 编译期校验**：

| 层 | 机制 |
|---|---|
| 元数据 | 存资源键（TitleKey 等），字符串仅作 fallback |
| 抽象 | `IDocStringProvider { string? Get(string key, CultureInfo); IObservable<CultureInfo> CultureChanges; }` |
| 内置实现 | `ResxDocStringProvider`（BCL `ResourceManager`，零第三方依赖） |
| Lingua 桥接 | 宿主自写几行适配代码（实现 `IDocStringProvider`），Dogma 不建适配包、不依赖 |
| 编译期 | SG 读 resx：键存在性校验（DOGDOC001）+ 默认文化文本烘焙（搜索索引/fallback） |
| 运行时 | `DocSite` 订阅 `CultureChanges` → 广播 `CultureChanged` → DocShell 重建导航标题 + 刷新搜索索引 |

### 关键权衡（烘焙 vs 动态解析）

- **烘焙**（编译期写死默认文化文本）：运行时零查找、AOT 安全、搜索索引稳定；不替代动态解析
- **动态解析**（provider 按当前文化查）：导航标题随语言切换，需 `CultureChanged` 广播
- 两者并存：显示走动态，搜索/fallback 走烘焙

### Lingua 桥接示例（仅示例代码，Dogma 不建适配包、不依赖）

Lingua 的 `LanguageManager.Instance` 是**惰性静态单例**（首次访问即构建就绪，
无初始化时序），provider 接入只需宿主一行代码；用 `Lazy` 延迟持有可彻底消除
接入时序约束：

```csharp
// 宿主项目中的普通 C# 类（运行时层；编译期 Lingua SG 产物已就绪，类型引用可解析）
public sealed class LinguaDocStringProvider : IDocStringProvider
{
    private readonly Lazy<ILinguaManager> _manager;

    public LinguaDocStringProvider(Func<ILinguaManager> factory) => _manager = new(factory);

    public string? Get(string key, CultureInfo culture) => _manager.Value.Resolve(key, culture);

    public IObservable<CultureInfo> CultureChanges => _manager.Value.CultureChanges;
}

// 接入：App 启动早期（OnFrameworkInitializationCompleted，窗口显示前）
GeneratedDocPages.Register(DocSite.Instance);
DocSite.Instance.SetStringProvider(new LinguaDocStringProvider(() => LanguageManager.Instance));
```

> 注：Dogma 的 SG 从不引用 Lingua（也无法引用——SG 之间看不到彼此生成结果），
> 只通过 `AdditionalFiles` 直接读 resx 做键校验与烘焙；`ILinguaManager` 的引用
> 只出现在宿主自己的桥接类里，同一编译单元内由 Lingua SG 生成。

## 分层实施计划

1. **元数据 + 注册表 + 搜索核心**（无 UI）：`DocPageAttribute`/`DocPageMetadata`/
   `DocSite`/`IDocStringProvider` + `ResxDocStringProvider` + 单元测试
   （树排序、搜索加权、provider fallback）
2. **Source Generator**：`DocPageGenerator`（扫描 + resx 键校验 + 烘焙）+
   Roslyn 单元测试（语法树驱动）+ 集成测试（示例页面编译后注册正确、键缺失报错）
3. **DocShell 控件**：树形侧栏 + 搜索过滤 + 内容区 + 文化切换刷新，
   ControlTheme 模板 + headless 测试
4. **demo 改造 + AOT 验证 + 文档**：demo 变文档站形态（多页面 + Attribute +
   多语言 resx）；`dotnet publish -p:PublishAot=true` 验证 NativeAOT 可发布运行

## 风险备注

- SG 读 resx 与 Lingua 输入协议一致但互不依赖；宿主两套都装时校验各自独立
- NativeAOT 下 `typeof(View)` 与工厂 new 均编译期确定，无反射；但宿主若在
  XAML 里用反射式 DataTemplate 需自行保证裁剪
- 运行时文化切换需要宿主在切换语言时同时更新 `DocSite` 的 provider 状态
