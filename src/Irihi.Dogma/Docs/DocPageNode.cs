namespace Irihi.Dogma.Docs;

/// <summary>
/// 树中的页面节点（导航/搜索的叶子单元）。
/// </summary>
public sealed class DocPageNode
{
    /// <summary>页面元数据。</summary>
    public required DocPageMetadata Metadata { get; init; }

    /// <summary>所属分类节点。</summary>
    public required DocCategoryNode Category { get; init; }
}
