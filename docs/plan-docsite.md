# 计划：Dogma 文档站基础设施（DocSite）

> 状态：设计修订版 4（共存模型）/ 分支：`feature/section-temp`
> 日期：2026-08-09

## 背景与目标

Dogma 将作为不同 Avalonia 控件库的**文档项目基础设施**。每个库需要基于
ViewModel 的声明式配置（Attribute），自动获得**完整的导航与搜索功能**，
满足 **NativeAOT** 发布要求。

## 设计原则（review 后修订）

1. **Dogma 直接依赖 Irihi.Lingua**：文档本地化以 Lingua 为唯一来源。
2. **文本消费走 `ILinguaManager.GetObservable(key)`**：元数据持资源键，
   运行时通过 Lingua 获取 `IObservable<string?>` 消费；不烘焙默认文本、
   不读 resx 做键校验（键正确性由 Lingua 自身保证）。
3. **共存模型**：一个 ViewModel 同时标 `[DocCategory]` 与 `[DocPage]`，
   即"该分类节点携带该页面"——**归属零声明**（无 CategoryKey / Pages /
   LandingPage 等冗余参数）。
4. **VM 声明关联 View，映射由 SG 生成**：`[DocPage]` 保留 `View = typeof(...)`
   （VM 知道自己关联的 View）；View 实现由各仓库自己写，VM→View 映射
   （ViewLocator / IDataTemplate）由 Source Generator 生成——静态映射、
   NativeAOT 标准（替代模板默认的反射版）。
5. **DocShell 外壳控件暂不实现**：导航树/搜索框 UI 由各仓库自行搭建。

## 架构：三层

```
┌─ 标记层    [DocCategory] + [DocPage]（标在同一 VM 上，共存 = 分类节点携带页面）
├─ 生成层    DocPageGenerator（Roslyn SG）→ ① 静态注册代码 ② GeneratedViewLocator（静态映射）
└─ 运行时层  DocSite 注册表 + 树构建/搜索 + GeneratedViewLocator（供宿主 ContentControl）
```

## 1. 标记层（共存模型）

```csharp
// 容器节点：IsClickable 显式声明可点击性（与层级无关，最上层也可点击）
// Key/TitleKey 为位置参数（只读属性不能作命名特性参数，C# CS0617）
[DocCategory("Docs.Controls", Order = 1, IsClickable = false,
            Tags = new[] { "group", "layout" })]

// 可点击节点：默认 IsClickable = true，无需显式；共存 = 分类节点携带页面
[DocCategory("Docs.Controls.Buttons", Parent = "Docs.Controls", Order = 1)]
[DocPage("Docs.Button.Title",
         View = typeof(ButtonView),          // VM 知道自己关联的 View
         Keywords = new[] { "click", "action" })]
public sealed partial class ButtonViewModel { }

// 多级/同级多个页面 = 分类树每个节点绑定一个页面（或纯容器）
[DocCategory("Docs.Controls.Input", Parent = "Docs.Controls", Order = 2)]
[DocPage("Docs.Input.Title", View = typeof(InputView))]
public sealed partial class InputViewModel { }
```

| Attribute | 参数 | 用途 |
|---|---|---|
| `[DocCategory]` | `Key` | 分类节点标识（Lingua 键作标题） |
| | `Parent` | 父分类键（顶层省略） |
| | `Order` | 同级排序 |
| | `IsClickable` | 是否可点击（显式声明，与层级/页面无关；**默认 true**） |
| | `Tags` | 标签数组（跨文化稳定，烘焙进 metadata，供应用需求消费：过滤/分组/UI 标记） |
| `[DocPage]` | `TitleKey` / `Title` | 页面标题键 + 可选 fallback 字面量 |
| | `View` | 关联 View 类型（供 GeneratedViewLocator 静态映射）；**可选**——仅标题/容器页面（如纯分类的落地页）可省略，不进 ViewLocator |
| | `Keywords` | 搜索关键字（不随文化变） |

**关联规则（唯一）**：两个 Attribute 标在同一 VM 上 = 该分类节点携带该页面。
没有 CategoryKey / Pages / LandingPage——归属零冗余声明。
**可点击性独立**：由 `IsClickable` 显式决定，与节点是否有父级/是否携带页面无关
（顶层节点、带页面的节点均可点击或不可点击）。

## 2. 生成层（AOT 关键）

`DocPageGenerator : IIncrementalGenerator`（netstandard2.0，不引用 Lingua 类型）：

### ① 静态注册代码

```csharp
// GeneratedDocPages.g.cs（自动生成）
public static partial class GeneratedDocPages
{
    public static void Register(IDocRegistry registry)
    {
        // 容器节点（IsClickable 显式声明）
        registry.AddCategory(new DocCategoryMetadata("Docs.Controls", parentKey: null, order: 1,
            isClickable: false, tags: new[] { "group" }));
        // 共存节点：一个 VM 同时注册分类与页面（Page 由 SG 从共存推断绑定）
        registry.AddCategory(new DocCategoryMetadata("Docs.Controls.Buttons", "Docs.Controls", 1,
            page: new DocPageMetadata(
                "Docs.Button.Title", typeof(ButtonViewModel), typeof(ButtonView),
                new[] { "click" }, null,
                viewModelFactory: () => new ButtonViewModel())));   // 编译期 new
    }
}
```

### ② GeneratedViewLocator（Avalonia ViewLocator，AOT 标准）

```csharp
// GeneratedViewLocator.g.cs（自动生成）
public sealed partial class GeneratedViewLocator : IDataTemplate
{
    public Control? Build(object? param) => param switch
    {
        ButtonViewModel => new ButtonView(),     // 静态 switch，零反射
        InputViewModel => new InputView(),
        _ => null,
    };
    public bool Match(object? data) => data is ButtonViewModel or InputViewModel ...;
}
```

- 替代 Avalonia 模板默认的反射版 ViewLocator（`Type.GetType` 在 NativeAOT 下
  会被裁剪）——相对生态惯例的进化点
- 宿主接入：`DataTemplates.Add(new GeneratedViewLocator())`（App 级），
  `ContentControl` 绑当前 VM 自动呈现对应 View
- 类型引用、`new` 调用全部编译期写死 → **NativeAOT 安全**；增量生成
- **编译期校验（SG 集成管理的价值）**：
  - 分类树**成环**（A→B→A）→ 编译错误 DOGDOC002
  - 分类 `Key` **重复声明** → 编译错误 DOGDOC003（Key 必须唯一）
  - `Parent` 指向**未声明**的 key → **不报错**：被引用即自动隐式创建容器节点
    （`IsClickable = false`、`Tags` 空，标题键 = key 本身，Lingua 无键时 fallback key 字面量）
- **不读 resx、不烘焙、不键校验**（键正确性由 Lingua 编译期保证）

## 数据模型（SG 生成代码与运行时对象）

### Attribute（用户声明，零冗余）

```csharp
namespace Irihi.Dogma.Docs;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DocCategoryAttribute(string key) : Attribute
{
    public string Key { get; } = key;         // 内部标识（Parent 链引用，非标题来源）
    public string? Parent { get; init; }      // 父分类键；null = 顶层
    public int Order { get; init; }           // 同级排序
    public bool IsClickable { get; init; }    // 显式可点击性（与层级/页面无关，默认 true）
    public string[]? Tags { get; init; }      // 标签（烘焙进 metadata，供应用消费）
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DocPageAttribute(string titleKey) : Attribute
{
    public string TitleKey { get; } = titleKey;   // Lingua 键（页面标题）
    public string? Title { get; init; }           // fallback 字面量（键缺失时兜底）
    public Type? View { get; init; }              // 关联 View（编译期 typeof）
    public string[]? Keywords { get; init; }      // 搜索关键字（跨文化稳定）
}
```

### Metadata（SG 生成代码构造的运行时对象）

```csharp
public sealed class DocCategoryMetadata
{
    public required string Key { get; init; }        // 内部标识（Parent 链引用，非标题来源）
    public string? ParentKey { get; init; }          // null = 顶层
    public int Order { get; init; }
    public bool IsClickable { get; init; } = true;   // 显式可点击性（默认 true）
    public IReadOnlyList<string> Tags { get; init; } = [];   // 供应用消费
    public bool IsExplicit { get; init; }            // false = 被引用而隐式创建的容器节点
    public DocPageMetadata? Page { get; init; }      // 共存 VM 的页面；null = 无页面
}

public sealed class DocPageMetadata
{
    public required string TitleKey { get; init; }
    public string? FallbackTitle { get; init; }
    public required Type ViewModelType { get; init; }
    public required Type ViewType { get; init; }             // 供 GeneratedViewLocator 映射
    public IReadOnlyList<string> Keywords { get; init; } = [];
    public required Func<object> ViewModelFactory { get; init; }   // AOT：() => new ButtonViewModel()
}
```

### 注册接口（GeneratedDocPages.Register 的调用目标）

```csharp
public interface IDocRegistry
{
    void AddCategory(DocCategoryMetadata category);   // 分类节点（Page 可空 = 无页面）
}
```

### 树模型（DocSite 构建后，供导航 UI 绑定）

```csharp
public sealed class DocCategoryNode
{
    public required DocCategoryMetadata Metadata { get; init; }
    public IObservable<string?> Title { get; init; }          // 共存页面标题（无页面容器 fallback Key），UI `^` 绑定
    public DocCategoryNode? Parent { get; init; }
    public IReadOnlyList<DocCategoryNode> Children { get; init; } = [];  // 已按 Order 排序
    public DocPageNode? Page { get; init; }                   // null = 无页面
    public bool IsClickable => Metadata.IsClickable;          // 显式声明，非推断
}

public sealed class DocPageNode
{
    public required DocPageMetadata Metadata { get; init; }
    public IObservable<string?> Title { get; init; }
    public required DocCategoryNode Category { get; init; }
}

public interface IViewModelProvider
{
    object GetViewModel(DocPageNode page);        // 语义：每次新建 / 单例缓存 / DI
}

public sealed class DocSite : IDocRegistry
{
    public static DocSite Instance { get; } = new();
    public ILinguaManager? LinguaManager { get; set; }            // 文本来源
    public IViewModelProvider ViewModelProvider { get; set; }     // 默认每次新建
    public IReadOnlyList<DocCategoryNode> Roots { get; }          // 顶层分类（排序后）
    public IEnumerable<DocPageNode> AllPages { get; }             // 树遍历收集的所有页面
    public IEnumerable<DocPageNode> Search(string query);         // 标题当前值 + Keywords 匹配
    public event Action? TreeChanged;                             // Register 后触发，宿主重建 UI
}
```

### 关键语义

- **文本不落 metadata**：`Title` 是运行时 `GetObservable(key)` 解析的 `IObservable<string?>`；
  metadata 只存键 + fallback
- **共存绑定**：`DocCategoryMetadata.Page` 由 SG 从"同一 VM 同时带两个 Attribute"推断填充，
  用户侧零关联声明
- **可点击性**：由 `Metadata.IsClickable` 显式声明（默认 true，与层级/页面无关）；隐式创建的容器节点
  `IsClickable = false`、`Tags` 为空
- **AOT**：`Func<object>` 工厂编译期生成、`typeof` 编译期写死

## 3. 运行时层

### DocSite（注册表 + 核心逻辑）

- `AddCategory`（由生成的 `GeneratedDocPages.Register` 调用；页面内嵌于分类元数据的 `Page`）
- 树构建：按 `Parent` 链组装分类树（任意深度），**未显式声明的分类节点运行时隐式创建**
  （`IsClickable = false` 的容器）；**共存 VM 的页面绑定到其分类节点**；同级按 `Order` 排序
- **VM 获取走 `IViewModelProvider`（创建能力与获取语义分离）**：
  - `DocPageMetadata.ViewModelFactory`（SG 生成 `() => new XxxViewModel()`）只是"创建能力"
  - `DocSite.ViewModelProvider` 决定"获取语义"：默认每次新建；宿主注入带缓存的实现
    （单例/DI）即得"从 cache 获取"，SG 注册代码一行不改
- `Navigate(page)`：经 provider 获取 VM 实例，暴露当前 VM
- `Search(query)`：消费 Lingua observable 的当前文化文本（标题/分类）+ Keywords

### 宿主集成（View 呈现由各仓库自建 UI）

- **内容呈现**：`ContentControl Content="{Binding CurrentViewModel}"` +
  `DataTemplates.Add(new GeneratedViewLocator())`——无需反射
- **导航/搜索 UI**：各仓库用自己的控件（TreeView/ListBox 等）绑定
  DocSite 提供的树数据 + 搜索查询；文本经 `IObservable<string?>` `^` 流绑定
  自动随文化切换

### 宿主接入

```csharp
// App.axaml.cs / OnFrameworkInitializationCompleted
GeneratedDocPages.Register(DocSite.Instance);
DocSite.Instance.LinguaManager = LanguageManager.Instance;
DataTemplates.Add(new GeneratedViewLocator());
```

## 4. 本地化（Lingua 驱动）

| 机制 | 说明 |
|---|---|
| 文本来源 | `ILinguaManager.GetObservable(key)` → `IObservable<string?>?`，键缺失返回 null |
| UI 消费 | 导航/搜索 UI 直接 `^` 流绑定 observable，文化切换自动刷新 |
| 搜索消费 | 订阅 observable 缓存当前文化值 + `Keywords`（跨文化稳定）匹配 |
| 键正确性 | Lingua 编译期生成强类型 `Keys` + 键一致性校验（LINGUA002/003），Dogma 不重复 |
| 全语言索引（可选） | `ILinguaManager.GetTranslations(LinguaObservableString)` 可拿全语言翻译，供搜索索引扩展 |

> 依赖形态：`Irihi.Dogma` 库直接 `PackageReference Irihi.Lingua`
> （运行时 `ILinguaManager` 接口 + 可观察字符串类型）。CodeBlock 等其他控件
> 不依赖 Lingua，仅 DocSite 部分消费。

## 分层实施计划

1. **元数据 + 注册表 + 搜索核心**（无 UI）：`DocPageAttribute`/`DocCategoryAttribute`/
   `DocPageMetadata`/`DocCategoryMetadata`/`DocSite` + 单元测试（树构建、排序、
   共存绑定、搜索加权、provider 语义）
2. **Source Generator**：`DocPageGenerator`（注册代码 + `GeneratedViewLocator`）+
   Roslyn 单元测试（语法树驱动）+ 集成测试（示例页面编译后注册正确、
   ViewLocator 静态映射命中、环/重复 Key 诊断）
3. **demo 改造 + AOT 验证 + 文档**：demo 变文档站形态（多页面 + Attribute +
   Lingua 多语言 + GeneratedViewLocator 呈现）；`dotnet publish -p:PublishAot=true`
   验证 NativeAOT 可发布运行
4. **（可选后续）DocShell 控件**：树形侧栏 + 搜索框 + 内容区的现成外壳，
   复用 CodeBlock 的 ControlTheme 模式

## 风险备注

- View 类型由 `[DocPage(View = typeof(...))]` 编译期指定（可选：仅标题/容器页面省略），GeneratedViewLocator 静态
  switch 一一映射（类型安全，无命名约定）；View 需公共无参构造（SG 生成 `new`，
  编译期强制）。仅当 ContentControl 收到**未注册的 VM 类型**时 `Build` 返回 null
  （预期 fallback，Avalonia 提示无匹配模板）
- Lingua `GetObservable` 对未知键返回 null：Attribute 的 `Title` fallback 兜底
- NativeAOT：VM/View 工厂均编译期 new，安全；宿主若额外用反射式模板需自行裁剪
