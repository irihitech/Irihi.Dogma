namespace Irihi.Dogma.Docs;

/// <summary>
/// VM 获取语义提供者：把"创建能力"（<see cref="DocPageMetadata.ViewModelFactory"/>）
/// 与"获取语义"（每次新建 / 单例缓存 / DI）分离。
/// </summary>
public interface IViewModelProvider
{
    /// <summary>获取（或创建）页面 VM 实例。</summary>
    object GetViewModel(DocPageNode page);
}

/// <summary>默认实现：每次导航都通过页面工厂新建实例（无缓存）。</summary>
public sealed class DefaultViewModelProvider : IViewModelProvider
{
    /// <inheritdoc />
    public object GetViewModel(DocPageNode page) => page.Metadata.ViewModelFactory();
}
