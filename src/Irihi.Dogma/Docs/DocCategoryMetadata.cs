namespace Irihi.Dogma.Docs;

/// <summary>
/// 分类节点的运行时元数据（由 Source Generator 生成的注册代码构造）。
/// </summary>
public sealed class DocCategoryMetadata
{
    /// <summary>分类节点标识（Lingua 键，标题来源）。</summary>
    public required string Key { get; init; }

    /// <summary>父分类键；null = 顶层。</summary>
    public string? ParentKey { get; init; }

    /// <summary>同级排序（同值按注册顺序，稳定）。</summary>
    public int Order { get; init; }

    /// <summary>显式可点击性（默认 true）。</summary>
    public bool IsClickable { get; init; } = true;

    /// <summary>标签（供应用消费）。</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>false = 被引用而隐式创建的容器节点。</summary>
    public bool IsExplicit { get; init; }

    /// <summary>共存 VM 的页面；null = 该节点无页面。</summary>
    public DocPageMetadata? Page { get; init; }
}
