namespace Irihi.Dogma.Docs;

/// <summary>
/// 标记一个分类树节点。与 <see cref="DocPageAttribute"/> 标在同一类型上时，
/// 该类型既是分类节点又携带页面（共存即关联）。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DocCategoryAttribute(string key) : Attribute
{
    /// <summary>分类节点标识（Lingua 键，作为标题来源）。</summary>
    public string Key { get; } = key;

    /// <summary>父分类键；null = 顶层节点。</summary>
    public string? Parent { get; init; }

    /// <summary>同级排序（同值按注册顺序，稳定）。</summary>
    public int Order { get; init; }

    /// <summary>是否可点击（显式声明，与层级/页面无关；默认 true）。</summary>
    public bool IsClickable { get; init; } = true;

    /// <summary>标签数组（烘焙进 metadata，供应用需求消费：过滤/分组/UI 标记）。</summary>
    public string[]? Tags { get; init; }
}
