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
// 纯分组节点（不可点击、无页面；未声明即被引用的父节点也会隐式创建）
[DocCategory(Key = "Docs.Controls", Order = 1)]

// 可点击节点：共存 = 分类节点携带页面，零归属声明
[DocCategory(Key = "Docs.Controls.Buttons", Parent = "Docs.Controls", Order = 1)]
[DocPage(TitleKey = "Docs.Button.Title",
         View = typeof(ButtonView),          // VM 知道自己关联的 View
         Keywords = new[] { "click", "action" })]
public sealed partial class ButtonViewModel { }

// 多级/同级多个页面 = 分类树每个节点绑定一个页面（或纯分组）
[DocCategory(Key = "Docs.Controls.Input", Parent = "Docs.Controls", Order = 2)]
[DocPage(TitleKey = "Docs.Input.Title", View = typeof(InputView))]
public sealed partial class InputViewModel { }
```

| Attribute | 参数 | 用途 |
|---|---|---|
| `[DocCategory]` | `Key` | 分类节点标识（Lingua 键作标题） |
| | `Parent` | 父分类键（顶层省略） |
| | `Order` | 同级排序 |
| `[DocPage]` | `TitleKey` / `Title` | 页面标题键 + 可选 fallback 字面量 |
| | `View` | 关联 View 类型（供 GeneratedViewLocator 静态映射） |
| | `Keywords` | 搜索关键字（不随文化变） |

**关联规则（唯一）**：两个 Attribute 标在同一 VM 上 = 该分类节点携带该页面。
没有 CategoryKey / Pages / LandingPage——归属零冗余声明。

## 2. 生成层（AOT 关键）

`DocPageGenerator : IIncrementalGenerator`（netstandard2.0，不引用 Lingua 类型）：

### ① 静态注册代码

```csharp
// GeneratedDocPages.g.cs（自动生成）
public static partial class GeneratedDocPages
{
    public static void Register(IDocRegistry registry)
    {
        // 纯分组
        registry.AddCategory(new DocCategoryMetadata("Docs.Controls", parentKey: null, order: 1));
        // 共存节点：一个 VM 同时注册分类与页面
        registry.AddCategory(new DocCategoryMetadata("Docs.Controls.Buttons", "Docs.Controls", 1));
        registry.AddPage(new DocPageMetadata(
            "Docs.Button.Title", typeof(ButtonView), new[] { "click" }, null,
            viewModelFactory: () => new ButtonViewModel()));   // 编译期 new
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
  - `Parent` 指向**未声明**的 key → **不报错**：被引用即自动隐式创建纯分组节点
    （标题键 = key 本身，Lingua 无键时 fallback key 字面量）
- **不读 resx、不烘焙、不键校验**（键正确性由 Lingua 编译期保证）

## 3. 运行时层

### DocSite（注册表 + 核心逻辑）

- `AddCategory`/`AddPage`（由生成的 `GeneratedDocPages.Register` 调用）
- 树构建：按 `Parent` 链组装分类树（任意深度），**未显式声明的分类节点运行时隐式创建**
  （纯分组、不可点击）；**共存 VM 的页面绑定到其分类节点**；同级按 `Order` 排序
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

- View 类型由 `[DocPage(View = typeof(...))]` 编译期指定，GeneratedViewLocator 静态
  switch 一一映射（类型安全，无命名约定）；View 需公共无参构造（SG 生成 `new`，
  编译期强制）。仅当 ContentControl 收到**未注册的 VM 类型**时 `Build` 返回 null
  （预期 fallback，Avalonia 提示无匹配模板）
- Lingua `GetObservable` 对未知键返回 null：Attribute 的 `Title` fallback 兜底
- NativeAOT：VM/View 工厂均编译期 new，安全；宿主若额外用反射式模板需自行裁剪
