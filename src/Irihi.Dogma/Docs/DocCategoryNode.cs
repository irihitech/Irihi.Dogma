namespace Irihi.Dogma.Docs;

/// <summary>
/// 分类树节点（可携带一个共存页面）。
/// 标题文本消费由宿主决定（可直接消费键，或自行接入本地化系统）。
/// </summary>
public sealed class DocCategoryNode
{
    /// <summary>分类元数据。</summary>
    public required DocCategoryMetadata Metadata { get; init; }

    /// <summary>父节点；null = 顶层。</summary>
    public DocCategoryNode? Parent { get; internal set; }

    /// <summary>子节点（已按 Order + 注册顺序稳定排序）。</summary>
    public IReadOnlyList<DocCategoryNode> Children { get; internal set; } = [];

    /// <summary>共存页面；null = 该节点无页面。</summary>
    public DocPageNode? Page { get; internal set; }

    /// <summary>是否可点击（显式声明，非推断）。</summary>
    public bool IsClickable => Metadata.IsClickable;
}
