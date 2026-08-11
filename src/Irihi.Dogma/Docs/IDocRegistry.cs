namespace Irihi.Dogma.Docs;

/// <summary>
/// 注册目标（由生成的 <c>GeneratedDocPages.Register</c> 调用）。
/// </summary>
public interface IDocRegistry
{
    /// <summary>注册一个分类节点（其 <see cref="DocCategoryMetadata.Page"/> 可空 = 无页面）。</summary>
    void AddCategory(DocCategoryMetadata category);
}
