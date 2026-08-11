namespace Irihi.Dogma.Docs;

/// <summary>
/// 标记一个文档页面（ViewModel）。与 <see cref="DocCategoryAttribute"/> 标在同一
/// 类型上时，该 VM 是其分类节点的页面（共存即关联）；仅标此 Attribute 的 VM
/// 不进入分类树。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DocPageAttribute(string titleKey) : Attribute
{
    /// <summary>Lingua 键（页面标题来源）。</summary>
    public string TitleKey { get; } = titleKey;

    /// <summary>可选 fallback 字面量（宿主消费 TitleKey 时可按需兜底）。</summary>
    public string? Title { get; init; }

    /// <summary>该 VM 关联的 View 类型（编译期 typeof，供 GeneratedViewLocator 静态映射）。</summary>
    public Type? View { get; init; }

    /// <summary>搜索关键字（跨文化稳定）。</summary>
    public string[]? Keywords { get; init; }
}
