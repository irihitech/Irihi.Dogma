namespace Irihi.Dogma.Docs;

/// <summary>
/// 文档页面的运行时元数据（由 Source Generator 生成的注册代码构造）。
/// </summary>
public sealed class DocPageMetadata
{
    /// <summary>Lingua 键（页面标题来源）。</summary>
    public required string TitleKey { get; init; }

    /// <summary>可选 fallback 字面量。</summary>
    public string? FallbackTitle { get; init; }

    /// <summary>页面 ViewModel 类型。</summary>
    public required Type ViewModelType { get; init; }

    /// <summary>关联 View 类型（供 GeneratedViewLocator 静态映射）。</summary>
    public required Type ViewType { get; init; }

    /// <summary>搜索关键字（跨文化稳定）。</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>创建 VM 实例的工厂（SG 生成：() =&gt; new XxxViewModel()，AOT 安全）。</summary>
    public required Func<object> ViewModelFactory { get; init; }
}
