# 计划：Dogma 文档站基础设施（DocSite）

> 状态：设计修订版（吸收 review 意见）/ 分支：`feature/section-temp`
> 日期：2026-08-09

## 背景与目标

Dogma 将作为不同 Avalonia 控件库的**文档项目基础设施**。每个库需要基于
ViewModel 的声明式配置（Attribute），自动获得**完整的导航与搜索功能**，
满足 **NativeAOT** 发布要求。

## 设计原则（review 后修订）

1. **Dogma 直接依赖 Irihi.Lingua**：文档本地化以 Lingua 为唯一来源，
   不做字符串源抽象层。
2. **文本消费走 `ILinguaManager.GetObservable(key)`**：元数据持资源键，
   运行时通过 Lingua 获取 `IObservable<string?>` 消费；不烘焙默认文本、
   不读 resx 做键校验（键正确性由 Lingua 自身保证）。
3. **只关注 ViewModel 层**：`[DocPage]` 标在 VM 上、不指定 View；
   View 的映射（DataTemplate/ViewLocator）由各仓库自己管理。

## 架构：三层

```
┌─ 标记层    [DocPage] Attribute（标在 VM：标题/分类/描述键 + 排序 + 关键字）
├─ 生成层    DocPageGenerator（Roslyn SG：编译期收集元数据 → 静态注册代码，AOT 安全）
└─ 运行时层  DocSite 注册表 + 树构建/搜索 + DocShell 控件（Lingua 驱动文本）
```

## 1. 标记层（ViewModel 上）

```csharp
[DocPage(TitleKey = "Docs.Button.Title",
         CategoryKey = "Docs.Controls",
         DescriptionKey = "Docs.Button.Desc",
         Order = 1,
         Keywords = new[] { "click", "action" })]
public sealed partial class ButtonViewModel { }
```

| 参数 | 用途 |
|---|---|
| `TitleKey` / `CategoryKey` / `DescriptionKey` | Lingua 资源键（导航/搜索/页面标题的文本来源） |
| `Title`（可选 fallback） | 键缺失时兜底显示的字面量，非 resx |
| `Order` | 分类内排序 |
| `Keywords` | 补充搜索关键字（不随文化变） |

**不包含**：View 类型（各仓库用 DataTemplate/ViewLocator 自己映射）。

## 2. 生成层（AOT 关键）

`DocPageGenerator : IIncrementalGenerator`（netstandard2.0，不引用 Lingua 类型）：

- 编译期扫描 `[DocPage]` → 生成静态注册代码：

```csharp
// GeneratedDocPages.g.cs（自动生成）
public static partial class GeneratedDocPages
{
    public static void Register(IDocRegistry registry) =>
        registry.AddPage(new DocPageMetadata(
            "Docs.Button.Title", "Docs.Controls", "Docs.Button.Desc",
            1, new[] { "click" }, null,
            viewModelFactory: () => new ButtonViewModel()));   // 编译期 new，AOT 裁剪安全
}
```

- **不读 resx、不烘焙、不键校验**（键正确性由 Lingua 编译期保证）
- 类型引用、`new` 调用全部编译期写死 → **NativeAOT 安全**；增量生成

## 3. 运行时层

### DocSite（注册表 + 核心逻辑）

- `AddPage(DocPageMetadata)`（由生成的 `GeneratedDocPages.Register` 调用）
- 树构建：`Category → Page`，按 `Order` 排序
- `Navigate(page)`：生成 VM 实例（编译期工厂），**不做 View 查找**；
  当前 VM 暴露给宿主，宿主用自己注册的 DataTemplate（ViewLocator）呈现
- `Search(query)`：消费 Lingua observable 的当前文化文本 + Keywords + 描述

### DocShell（控件，复用 CodeBlock 的 ControlTheme 模式）

- 左侧树形侧栏（分类可折叠）：标题绑定 `IObservable<string?>`（`^` 流绑定，
  文化切换自动刷新）
- 搜索框：输入即过滤树 + 空结果提示
- 内容区 `ContentControl` 绑当前 VM → 宿主 DataTemplate 渲染对应 View
- 文化切换：Lingua `CultureChanges` 触发重建/刷新导航标题

### 宿主接入

```csharp
// App.axaml.cs / OnFrameworkInitializationCompleted
GeneratedDocPages.Register(DocSite.Instance);
DocSite.Instance.LinguaManager = LanguageManager.Instance;   // 文本来源
```

## 4. 本地化（Lingua 驱动）

| 机制 | 说明 |
|---|---|
| 文本来源 | `ILinguaManager.GetObservable(key)` → `IObservable<string?>?`，键缺失返回 null |
| UI 消费 | DocShell/导航标题直接 `^` 流绑定 observable，文化切换自动刷新 |
| 搜索消费 | 订阅 observable 缓存当前文化值 + `Keywords`（跨文化稳定）匹配 |
| 键正确性 | Lingua 编译期生成强类型 `Keys` + 键一致性校验（LINGUA002/003），Dogma 不重复 |
| 全语言索引（可选） | `ILinguaManager.GetTranslations(LinguaObservableString)` 可拿全语言翻译，供搜索索引扩展 |

> 依赖形态：`Irihi.Dogma` 库直接 `PackageReference Irihi.Lingua`
> （运行时 `ILinguaManager` 接口 + 可观察字符串类型）。CodeBlock 等其他控件
> 不依赖 Lingua，仅 DocSite 部分消费。

## 分层实施计划

1. **元数据 + 注册表 + 搜索核心**（无 UI）：`DocPageAttribute`/`DocPageMetadata`/
   `DocSite` + 单元测试（树排序、搜索加权、Lingua observable 消费）
2. **Source Generator**：`DocPageGenerator`（扫描生成注册代码）+ Roslyn 单元测试 +
   集成测试（示例页面编译后注册正确）
3. **DocShell 控件**：树形侧栏 + 搜索过滤 + 内容区 + 文化切换刷新，
   ControlTheme 模板 + headless 测试
4. **demo 改造 + AOT 验证 + 文档**：demo 变文档站形态（多页面 + Attribute +
   Lingua 多语言）；`dotnet publish -p:PublishAot=true` 验证 NativeAOT 可发布运行

## 风险备注

- DocShell 内容区依赖宿主的 DataTemplate/ViewLocator 呈现 View——宿主需
  提供全局 DataTemplate（Avalonia 模板自带 ViewLocator 模式）
- Lingua `GetObservable` 对未知键返回 null：Attribute 的 `Title` fallback 兜底，
  避免空标题
- NativeAOT：VM 工厂编译期 new，安全；宿主的 View 侧需自行保证其 DataTemplate
  路径裁剪安全
